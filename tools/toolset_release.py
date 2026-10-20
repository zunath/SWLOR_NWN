"""Local and CI toolset release: checkout guards, Windows plus Docker-Linux build, and idempotent gh publishing.

One implementation serves ReleaseToolset.cmd (the owner's machine, no GitHub Actions) and the publish job of
.github/workflows/release-toolset.yml.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import shlex
import shutil
import ssl
import subprocess
import sys
import time

from toolset_common import ROOT, run

REPO = 'zunath/SWLOR_NWN'
OWNER_LOGIN = 'zunath'
LATEST_TAG = 'toolset-latest'
LATEST_TITLE = 'SWLOR Toolset (current)'
VERSION_PATTERN = r'\d+\.\d+\.\d+(-[A-Za-z0-9.-]+)?'
DOCKERFILE = ROOT / 'tools/toolset-release/Dockerfile'
SIGN_PFX_VARIABLE = 'SWLOR_SIGN_PFX'
SIGN_PASSWORD_VARIABLE = 'SWLOR_SIGN_PASSWORD'
# Mirrors the filters in .github/workflows/release-toolset.yml (a test keeps them identical).
TOOLSET_TEST_CLASSES = ('ContributionArchiveTests', 'WorkspaceDownloadTests', 'GameCodeCatalogTests', 'AssetManifestTests',
                        'ToolsetFeedTests', 'PackServiceTests', 'HakSetupValidationTests', 'WorkspaceSetupWindowTests',
                        'AppRepositoryRootTests', 'ModuleCustomContentServiceTests', 'ScriptCompilerTests',
                        'ToolsetStartupViewTests', 'ErfArchiveServiceTests', 'NativeGffInputTests', 'NativeToolPathTests',
                        'LinuxGameDiscoveryTests', 'NwnIniProfileTests')
SETUP_TEST_CLASSES = ('SetupOptionsTests', 'NativeToolsTests', 'SetupApplicationTests', 'CommandRunnerTests')
PYTHON_TEST_MODULES = ('tools.tests.test_toolset_content', 'tools.tests.test_toolset_distribution', 'tools.tests.test_toolset_release')
RELEASE_NOTES = 'Standalone Windows and Linux toolsets, shared contributor workspace, and matching asset downloads.'


def is_windows():
    return os.name == 'nt'


def versioned_tag(version):
    return f'toolset-v{version}'


def is_prerelease(version):
    return '-' in version


def class_filter(names):
    return '|'.join('FullyQualifiedName~' + name for name in names)


def file_sha256(path):
    with Path(path).open('rb') as source:
        return hashlib.file_digest(source, 'sha256').hexdigest()


# ----------------------------------------------------------------------------------------------------------------
# gh and git access. Tests replace run_gh and git.
# ----------------------------------------------------------------------------------------------------------------

class GhError(RuntimeError):
    def __init__(self, arguments, stderr):
        super().__init__(f'gh {" ".join(str(a) for a in arguments[:3])} failed: {stderr.strip()}')
        self.stderr = stderr


def run_gh(arguments, check=True):
    """Run gh. With check, return stdout or raise; otherwise return (code, stdout, stderr). gh reads its own credentials."""
    result = subprocess.run(['gh', *[str(a) for a in arguments]], text=True, capture_output=True, timeout=3600)
    if not check:
        return result.returncode, result.stdout, result.stderr
    if result.returncode:
        raise GhError(arguments, result.stderr or result.stdout)
    return result.stdout


def git(*args):
    return subprocess.check_output(['git', '-C', str(ROOT), *args], text=True).strip()


# ----------------------------------------------------------------------------------------------------------------
# Guards
# ----------------------------------------------------------------------------------------------------------------

def parse_gh_auth(status):
    """Return (login, scopes) of the active gh account for github.com."""
    for block in re.split(r'(?=Logged in to )', status):
        login = re.match(r'Logged in to github\.com account (\S+)', block)
        if login and re.search(r'Active account:\s*true', block):
            scopes = re.search(r'Token scopes:\s*(.*)', block)
            return login.group(1), set(re.findall(r'[\w:.-]+', scopes.group(1))) if scopes else set()
    return None, set()


def check_gh_login():
    _, out, err = run_gh(['auth', 'status', '--hostname', 'github.com'], check=False)
    login, scopes = parse_gh_auth(out + '\n' + err)
    if login is None:
        raise RuntimeError('gh is not logged in to github.com. Run: gh auth login')
    if login.lower() != OWNER_LOGIN:
        raise RuntimeError(f'gh is logged in as {login}; releases are published by {OWNER_LOGIN}. Run: gh auth switch')
    if 'repo' not in scopes:
        raise RuntimeError('The gh token needs the repo scope. Run: gh auth refresh -s repo')
    return login


def check_checkout(allow_dev_checkout=False):
    """The checkout must be master, clean, and equal to origin/master. Returns the commit that will be released."""
    branch = git('rev-parse', '--abbrev-ref', 'HEAD')
    dirty = git('status', '--porcelain')
    # Git for Windows needs the Windows certificate store for github.com on some machines.
    prefix = ('-c', 'http.sslBackend=schannel') if is_windows() else ()
    try:
        git(*prefix, 'fetch', '--quiet', 'origin', '+refs/heads/master:refs/remotes/origin/master')
    except subprocess.CalledProcessError as error:
        raise RuntimeError('Could not fetch origin/master to compare against. Check your network and git credentials.') from error
    head = git('rev-parse', 'HEAD')
    origin = git('rev-parse', 'refs/remotes/origin/master')
    problems = []
    if branch != 'master':
        problems.append(f'is on branch {branch}, not master')
    if dirty:
        problems.append('has uncommitted changes')
    if head != origin:
        problems.append('is not equal to origin/master (pull or push first)')
    if problems and not allow_dev_checkout:
        raise RuntimeError('Refusing to build a release: the checkout ' + ' and '.join(problems) + '.')
    if problems:
        print('WARNING: --allow-dev-checkout: the checkout ' + ' and '.join(problems) + '. Dry run only.')
    return head


def check_signing_environment(environ=None):
    environ = os.environ if environ is None else environ
    pfx = environ.get(SIGN_PFX_VARIABLE)
    if not pfx or not environ.get(SIGN_PASSWORD_VARIABLE):
        raise RuntimeError(f'Signing was requested. Set {SIGN_PFX_VARIABLE} (PFX path) and {SIGN_PASSWORD_VARIABLE} in the environment.')
    if not Path(pfx).is_file():
        raise RuntimeError(f'The signing PFX named by {SIGN_PFX_VARIABLE} was not found.')


# ----------------------------------------------------------------------------------------------------------------
# Release assets and publishing
# ----------------------------------------------------------------------------------------------------------------

def release_files(directory):
    files = sorted(path for path in Path(directory).iterdir() if path.is_file())
    if not files:
        raise RuntimeError(f'No release files found in {directory}.')
    return files


def merge_platform_releases(sources, destination):
    """Copy each platform's release folder into one folder, refusing duplicate names."""
    destination = Path(destination)
    destination.mkdir(parents=True)
    for source in sources:
        for path in release_files(source):
            target = destination / path.name
            if target.exists():
                raise RuntimeError(f'Two platforms produced the same release file: {path.name}')
            shutil.copy2(path, target)
    return release_files(destination)


def is_user_download(path):
    """Files shown on the stable download page. Velopack update indexes and packages stay on the versioned release."""
    name = Path(path).name.lower()
    return name == 'toolset-feed.json' or name.endswith(('.exe', '.zip', '.appimage', '.tar.gz'))


def remote_assets(release):
    return {asset['name']: asset for asset in release.get('assets', [])}


def same_asset(local, remote):
    """Compare by size and SHA-256. An asset without a reported digest cannot be verified, so it never matches."""
    digest = remote.get('digest')
    return bool(digest) and remote.get('size') == local.stat().st_size and digest.lower() == 'sha256:' + file_sha256(local)


def gh_release(tag):
    """The release for a tag, or None. Drafts are invisible to `release view` by tag, so a hidden draft is refused."""
    code, out, err = run_gh(['release', 'view', tag, '--repo', REPO, '--json', 'assets,isDraft,isImmutable,isPrerelease'], check=False)
    if code == 0:
        return json.loads(out)
    if 'not found' not in (err + out).lower():
        raise GhError(['release', 'view', tag], err or out)
    listing = json.loads(run_gh(['release', 'list', '--repo', REPO, '--limit', '200', '--json', 'tagName,isDraft']) or '[]')
    if any(item['tagName'] == tag and item['isDraft'] for item in listing):
        raise RuntimeError(f'An unfinished draft release for {tag} exists. Delete that draft on GitHub (or: gh release delete {tag}) and rerun.')
    return None


class Publisher:
    def __init__(self, dry_run=True, log=print):
        self.dry_run = dry_run
        self.log = log

    def write(self, arguments):
        if self.dry_run:
            self.log('[dry-run] gh ' + ' '.join(self._display(a) for a in arguments))
            return ''
        return run_gh(arguments)

    @staticmethod
    def _display(argument):
        text = str(argument)
        return f'"{text}"' if ' ' in text else text

    def publish_versioned(self, version, files, revision):
        """Create toolset-v{version} without taking Latest from game releases; skip identical, refuse different."""
        tag = versioned_tag(version)
        release = gh_release(tag)
        existing_tag = self.tag_commit(tag)
        if existing_tag is not None and existing_tag != revision:
            raise RuntimeError(f'The tag {tag} already points at {existing_tag[:12]}, not {revision[:12]}. Published versions are never '
                               'moved: publish a new version, or delete the stale tag and release if they were never used.')
        if release is None:
            options = ['--target', revision, '--title', f'SWLOR Toolset {version}', '--notes', RELEASE_NOTES, '--latest=false']
            if is_prerelease(version):
                options.append('--prerelease')
            self.write(['release', 'create', tag, *files, '--repo', REPO, *options])
            self.log(f'{tag}: created with {len(files)} assets (not marked latest).')
            return
        remote = remote_assets(release)
        by_name = {path.name: path for path in files}
        mismatched = [name for name, asset in remote.items() if name not in by_name or not same_asset(by_name[name], asset)]
        if mismatched:
            raise RuntimeError(f'{tag} already exists with different assets ({", ".join(sorted(mismatched))}). '
                               'Published versions are never replaced: publish a new version instead.')
        missing = [path for path in files if path.name not in remote]
        if missing:
            self.write(['release', 'upload', tag, *missing, '--repo', REPO])
            self.log(f'{tag}: uploaded {len(missing)} missing assets to an interrupted release.')
        if release.get('isDraft'):
            # gh publishes a draft only after every upload succeeds, so a draft is an interrupted release: finish it.
            self.write(['release', 'edit', tag, '--repo', REPO, '--draft=false', '--latest=false'])
            self.log(f'{tag}: published the recovered draft (not marked latest).')
        elif not missing:
            self.log(f'{tag}: already published with identical assets; skipped.')

    @staticmethod
    def tag_commit(tag):
        """The commit a tag points at, or None when the tag does not exist."""
        code, out, err = run_gh(['api', f'repos/{REPO}/commits/{tag}', '--jq', '.sha'], check=False)
        if code == 0:
            return out.strip()
        if re.search(r'404|422|not found|no commit found', err + out, re.I):
            return None
        raise GhError(['api', 'commits', tag], err or out)

    def move_latest_tag(self, revision):
        code, out, err = run_gh(['api', f'repos/{REPO}/git/ref/tags/{LATEST_TAG}', '--jq', '.object.sha'], check=False)
        if code == 0:
            if out.strip() != revision:
                self.write(['api', '--method', 'PATCH', f'repos/{REPO}/git/refs/tags/{LATEST_TAG}', '-f', f'sha={revision}', '-F', 'force=true'])
        elif 'not found' in (err + out).lower() or '404' in (err + out):
            self.write(['api', '--method', 'POST', f'repos/{REPO}/git/refs', '-f', f'ref=refs/tags/{LATEST_TAG}', '-f', f'sha={revision}'])
        else:
            raise GhError(['api', 'ref'], err or out)

    def publish_current(self, version, files, revision):
        """Maintain the fixed, never-latest toolset-latest release: tag moved, downloads clobbered, stale assets removed."""
        downloads = [path for path in files if is_user_download(path)]
        notes = (f'The current stable SWLOR Toolset is {version}. Download it here; the toolset updates itself from the '
                 f'versioned releases (toolset-v{version}). Every stable toolset release replaces this one.')
        release = gh_release(LATEST_TAG)
        if release is not None and release.get('isImmutable'):
            raise RuntimeError(f'{LATEST_TAG} is immutable, so it cannot be replaced. Disable immutable releases for this repository, '
                               f'or delete the {LATEST_TAG} release and tag.')
        self.move_latest_tag(revision)
        if release is None:
            self.write(['release', 'create', LATEST_TAG, '--repo', REPO, '--verify-tag', '--title', LATEST_TITLE,
                        '--notes', notes, '--latest=false'])
        else:
            self.write(['release', 'edit', LATEST_TAG, '--repo', REPO, '--title', LATEST_TITLE, '--notes', notes,
                        '--latest=false', '--prerelease=false', '--draft=false'])
        # --clobber deletes before it uploads, so only touch assets that actually differ.
        remote = remote_assets(release or {})
        changed = [path for path in downloads if path.name not in remote or not same_asset(path, remote[path.name])]
        if changed:
            self.write(['release', 'upload', LATEST_TAG, *changed, '--repo', REPO, '--clobber'])
        keep = {path.name for path in downloads}
        for name in sorted(set(remote) - keep):
            self.write(['release', 'delete-asset', LATEST_TAG, name, '--repo', REPO, '--yes'])
        self.log(f'{LATEST_TAG}: points at {revision[:12]} with {len(downloads)} download assets (not marked latest).')


def publish_release(version, assets_directory, revision, dry_run=True, log=print):
    if not re.fullmatch(VERSION_PATTERN, version):
        raise RuntimeError('Use a semantic version such as 1.0.0.')
    if not re.fullmatch(r'[0-9a-f]{40}', revision):
        raise RuntimeError('The source revision must be a full commit hash.')
    files = release_files(assets_directory)
    if 'toolset-feed.json' not in {path.name for path in files}:
        raise RuntimeError('The release has no toolset-feed.json; the Windows build did not produce a workspace.')
    publisher = Publisher(dry_run, log)
    log(('Dry run: nothing is created, tagged, or uploaded. ' if dry_run else 'Publishing ') + f'toolset {version} from {revision[:12]}:')
    for path in files:
        log(f'  {path.name}  {path.stat().st_size / 1048576:.1f} MiB  sha256 {file_sha256(path)[:16]}...')
    publisher.publish_versioned(version, files, revision)
    if is_prerelease(version):
        log(f'{LATEST_TAG}: unchanged (prerelease versions never become the current release).')
    else:
        publisher.publish_current(version, files, revision)


# ----------------------------------------------------------------------------------------------------------------
# Local build
# ----------------------------------------------------------------------------------------------------------------

def powershell(script, timeout=120):
    return run(['powershell', '-NoProfile', '-NonInteractive', '-Command', script], capture=True, timeout=timeout)


def import_signing_certificate():
    """Import the PFX into the current user's store for vpk. Returns (sign parameters, cleanup)."""
    script = (f"$p = ConvertTo-SecureString $env:{SIGN_PASSWORD_VARIABLE} -AsPlainText -Force; "
              f"$t = (Get-PfxData -FilePath $env:{SIGN_PFX_VARIABLE} -Password $p).EndEntityCertificates[0].Thumbprint; "
              "$existed = Test-Path \"Cert:\\CurrentUser\\My\\$t\"; "
              f"if (!$existed) {{ Import-PfxCertificate -FilePath $env:{SIGN_PFX_VARIABLE} -Password $p -CertStoreLocation Cert:\\CurrentUser\\My | Out-Null }}; "
              "\"$t $existed\"")
    thumbprint, existed = powershell(script).split()[-2:]
    if not re.fullmatch(r'[0-9A-Fa-f]{40}', thumbprint):
        raise RuntimeError('Could not read the signing certificate thumbprint.')

    def cleanup():
        if existed != 'True':  # Never remove a certificate the machine already had.
            powershell(f'Remove-Item -LiteralPath "Cert:\\CurrentUser\\My\\{thumbprint}"')
    return f'/sha1 {thumbprint} /fd SHA256 /tr https://timestamp.digicert.com /td SHA256', cleanup


def resolve_asset_descriptor(asset_revision, directory):
    destination = Path(directory) / 'assets'
    try:
        run_gh(['release', 'download', f'toolset-assets-{asset_revision}', '--repo', REPO, '--pattern', 'asset-pack.json', '--dir', destination])
    except GhError as error:
        raise RuntimeError(f'Publish the asset pack for {asset_revision} first (tools/ToolsetDistribution.py assets, uploaded to the '
                           f'toolset-assets-{asset_revision} release, not latest), or pass --asset-descriptor.') from error
    return destination / 'asset-pack.json'


def test_commands():
    build = ['-c', 'Release', '-p:RunPostBuildEvent=Never']
    toolset = 'SWLOR.Toolset.Tests/SWLOR.Toolset.Tests.csproj'
    setup = 'scripts/SWLOR.ContributorSetup.Tests/SWLOR.ContributorSetup.Tests.csproj'
    return [['dotnet', 'build', toolset, *build],
            ['dotnet', 'test', toolset, '-c', 'Release', '--no-build', '--filter', class_filter(TOOLSET_TEST_CLASSES)],
            ['dotnet', 'build', setup, *build],
            ['dotnet', 'test', setup, '-c', 'Release', '--no-build', '--filter', class_filter(SETUP_TEST_CLASSES)],
            ['python', '-B', '-m', 'unittest', *PYTHON_TEST_MODULES]]


def export_host_trust(directory, certificates=None):
    """Write the Windows root CAs as PEM files so the container trusts what this machine trusts (for example the
    TLS-scanning root of an antivirus, without which NuGet and github.com downloads fail inside the container)."""
    if certificates is None:
        certificates = ssl.enum_certificates('ROOT') if hasattr(ssl, 'enum_certificates') else []
    server_auth = '1.3.6.1.5.5.7.3.1'
    directory = Path(directory)
    directory.mkdir(parents=True, exist_ok=True)
    count = 0
    for der, encoding, trust in certificates:
        if encoding == 'x509_asn' and (trust is True or server_auth in trust):
            count += 1
            (directory / f'host-root-{count:04d}.crt').write_text(ssl.DER_cert_to_PEM_cert(der), encoding='ascii')
    return count


def linux_container_script(run_tests):
    lines = ['set -euo pipefail',
             'if ls /in/host-ca/*.crt >/dev/null 2>&1; then cp /in/host-ca/*.crt /usr/local/share/ca-certificates/ && update-ca-certificates >/dev/null; fi',
             'mkdir -p /work/src && tar -xf /in/source.tar -C /work/src && cd /work/src',
             'python3 -B tools/ToolsetDistribution.py prepare-tools']
    if run_tests:
        lines.append('export SWLOR_REQUIRE_NATIVE_TOOLS=1')
        lines += [' '.join(shlex.quote(part if part != 'python' else 'python3') for part in command) for command in test_commands()]
    lines += ['python3 -B tools/ToolsetDistribution.py publish --version "$TOOLSET_VERSION" --skip-workspace --output /work/build',
              'python3 -B tools/VerifyToolsetInstallation.py /work/build/application /work/install-check.json',
              'image=$(find /work/build/release -maxdepth 1 -name "*.AppImage" -print -quit)', 'test -n "$image"',
              'APPIMAGE_EXTRACT_AND_RUN=1 "$image" --verify-install /work/appimage-check.json',
              'xvfb-run -a python3 -B tools/VerifyToolsetInstallation.py /work/build/application /work/linux-desktop-check.json --desktop',
              'cp /work/build/release/* /out/']
    return '\n'.join(lines)


def docker_image_tag():
    return 'swlor-toolset-linux-build:' + hashlib.sha256(DOCKERFILE.read_bytes()).hexdigest()[:12]


def build_linux(version, output, work, run_tests):
    """Package linux-x64 in the pinned container from a clean `git archive` of HEAD (never the Windows working tree)."""
    try:
        run(['docker', 'info', '--format', '{{.OSType}}'], capture=True, timeout=60)
    except (RuntimeError, OSError, subprocess.SubprocessError) as error:
        raise RuntimeError('Start Docker Desktop (Linux containers) and rerun. The Linux AppImage and tar.gz are built in a container.') from error
    tag = docker_image_tag()
    run(['docker', 'build', '--tag', tag, '--file', DOCKERFILE, DOCKERFILE.parent], timeout=3600)
    source = work / 'in'
    source.mkdir(parents=True)
    run(['git', 'archive', '--format=tar', '--output', source / 'source.tar', 'HEAD'])
    export_host_trust(source / 'host-ca')
    output.mkdir(parents=True)
    run(['docker', 'run', '--rm', '--env', 'TOOLSET_VERSION=' + version,
         '--volume', f'{source}:/in:ro', '--volume', f'{output}:/out', '--volume', 'swlor-toolset-nuget:/root/.nuget/packages',
         tag, 'bash', '-c', linux_container_script(run_tests)], timeout=None)


def build_windows(version, output, descriptor, sign_parameters, run_tests):
    script = ROOT / 'tools/ToolsetDistribution.py'
    run([sys.executable, '-B', script, 'prepare-tools'])
    if run_tests:
        environment = os.environ.copy()
        environment['SWLOR_REQUIRE_NATIVE_TOOLS'] = '1'
        for command in test_commands():
            run([sys.executable if part == 'python' else part for part in command], env=environment, timeout=None)
    arguments = [sys.executable, '-B', script, 'publish', '--version', version, '--asset-descriptor', descriptor, '--output', output]
    if sign_parameters:
        arguments += ['--sign-parameters', sign_parameters]
    run(arguments, timeout=None)
    run([sys.executable, '-B', ROOT / 'tools/VerifyToolsetInstallation.py', output / 'application', output.parent / 'windows-install-check.json'], timeout=300)


def directory_size(directory):
    return sum(path.stat().st_size for path in Path(directory).rglob('*') if path.is_file())


def release_local(args):
    if not is_windows():
        raise RuntimeError('ReleaseToolset builds the Windows installer, so it runs on Windows. Linux packages are built in Docker.')
    if not re.fullmatch(VERSION_PATTERN, args.version):
        raise RuntimeError('Use a semantic version such as 1.0.0.')
    if args.publish and args.allow_dev_checkout:
        raise RuntimeError('--allow-dev-checkout is for dry runs only.')
    started = time.monotonic()
    if args.sign_windows:
        check_signing_environment()
    check_gh_login()
    revision = check_checkout(args.allow_dev_checkout)
    output = Path(args.output) if args.output else Path.home() / 'swlor-toolset-releases' / f'{args.version}-{revision[:8]}'
    output = output.resolve()
    if output == ROOT or ROOT in output.parents:
        raise RuntimeError('Choose an output directory outside the source checkout.')
    release = output / 'release'
    if output.exists():
        files = reusable_build(output, args.version, revision, args.publish, not args.skip_tests, args.sign_windows)
        print(f'Reusing the verified build of {args.version} at {revision[:12]} in {release}; nothing is rebuilt.')
    else:
        files = build_release(args, output, revision)
    print(f'{len(files)} release files in {release} ({directory_size(release) / 1048576:.0f} MiB; '
          f'output folder {directory_size(output) / 1048576:.0f} MiB).')
    publish_release(args.version, release, revision, dry_run=not args.publish)
    print(f'Total {(time.monotonic() - started) / 60:.1f} min.')
    if not args.publish:
        print(f'Dry run complete. Rerun the same command with --publish to release this build without rebuilding it. Delete {output} when done.')


def build_info_path(output):
    return Path(output) / 'build-info.json'


def reusable_build(output, version, revision, publishing=False, need_tests=False, need_signed=False):
    """The release files of an earlier, complete build of this version and commit (a dry run), or an error.
    Publishing never reuses a dev-checkout build, an untested build when tests are required, or an unsigned one when signing is."""
    try:
        info = json.loads(build_info_path(output).read_text(encoding='utf-8'))
        files = release_files(Path(output) / 'release')
        recorded = info['Files']
    except (OSError, ValueError, KeyError, RuntimeError):
        recorded = None
    if recorded is None or info.get('Version') != version or info.get('Revision') != revision             or {path.name: file_sha256(path) for path in files} != recorded:
        raise RuntimeError(f'{output} already exists and is not an intact, complete build of {version} at {revision[:12]}. '
                           'Choose another --output or delete it.')
    shortfalls = []
    if publishing and info.get('DevCheckout'):
        shortfalls.append('it was built from a checkout that bypassed the master/clean/origin guards')
    if need_tests and not info.get('Tested'):
        shortfalls.append('it was built with --skip-tests')
    if need_signed and not info.get('Signed'):
        shortfalls.append('it was not signed')
    if shortfalls:
        raise RuntimeError(f'Cannot publish the existing build in {output}: ' + ' and '.join(shortfalls) + '. Delete it and rerun.')
    return files


def build_release(args, output, revision):
    work = output / '.work'
    work.mkdir(parents=True)
    timings = {}
    cleanup = None
    try:
        descriptor = Path(args.asset_descriptor).resolve() if args.asset_descriptor else resolve_asset_descriptor(git('rev-parse', 'HEAD:SWLOR_Haks'), work)
        sign_parameters = None
        if args.sign_windows:
            sign_parameters, cleanup = import_signing_certificate()
        stage = time.monotonic()
        build_windows(args.version, output / 'win-x64', descriptor, sign_parameters, not args.skip_tests)
        timings['win-x64'] = time.monotonic() - stage
        stage = time.monotonic()
        build_linux(args.version, output / 'linux-x64-release', work, not args.skip_tests)
        timings['linux-x64'] = time.monotonic() - stage
    finally:
        if cleanup:
            cleanup()
    files = merge_platform_releases([output / 'win-x64/release', output / 'linux-x64-release'], output / 'release')
    shutil.rmtree(work, ignore_errors=True)
    build_info_path(output).write_text(json.dumps({'Version': args.version, 'Revision': revision, 'DevCheckout': args.allow_dev_checkout,
                                                   'Tested': not args.skip_tests, 'Signed': args.sign_windows,
                                                   'Files': {path.name: file_sha256(path) for path in files}}, indent=2), encoding='utf-8')
    print('Timing: ' + ', '.join(f'{name} {seconds / 60:.1f} min' for name, seconds in timings.items()))
    return files
