"""Create a separate NWN:EE Windows client with DDS-aware composite icon discovery.

Never edits the source executable. Only the explicitly verified client build is
accepted. This is a client patch, not a HAK or server-side fix. See
SWLOR.Game.Server/Readmes/BlasterModelImport.md for installation and limitations.
"""
import argparse
import hashlib
from pathlib import Path
import struct


SOURCE_SHA256 = "3b7cb1252e0edb2ce22d7971f333aade027039ae30a45b4bc64732c3e6bec73a"
BUILD = "8193.37-17 / 26c6e573 / Windows x64"
ICON_EXISTS_CALL_RVA = 0x84069C
RESOURCE_EXISTS_RVA = 0x18F590
ORIGINAL_CALL = bytes.fromhex("e8efee94ff")
SECTION_NAME = b".ddsicn"


def align(value, alignment):
    return (value + alignment - 1) // alignment * alignment


def make_lookup_thunk(thunk_rva, exists_rva=RESOURCE_EXISTS_RVA):
    """Windows x64 ABI: preserve all four arguments across the first Exists call.

    Keep the original TGA result when it succeeds. Only a missing TGA causes a
    second lookup for the same resref as DDS (2033). Both missing still returns 0
    to the original generic-icon fallback. No resources are fabricated.
    """
    code = bytearray.fromhex(
        "48 83 ec 48 "       # sub rsp, 72: shadow space, saved args, alignment
        "48 89 4c 24 20 "    # save rcx
        "48 89 54 24 28 "    # save rdx
        "4c 89 44 24 30 "    # save r8
        "4c 89 4c 24 38"     # save r9
    )

    def call_exists():
        code.append(0xE8)
        code.extend(struct.pack("<i", exists_rva - (thunk_rva + len(code) + 4)))

    call_exists()
    code.extend(bytes.fromhex("85 c0 75 00"))  # test eax,eax; jne done
    success_jump = len(code) - 1
    code.extend(bytes.fromhex("83 7c 24 30 03 75 00"))  # saved type == TGA?
    other_type_jump = len(code) - 1
    code.extend(bytes.fromhex(
        "48 8b 4c 24 20 "    # restore rcx
        "48 8b 54 24 28 "    # restore rdx
        "41 b8 f1 07 00 00 " # mov r8d, 2033 (DDS)
        "4c 8b 4c 24 38"     # restore r9
    ))
    call_exists()
    done = len(code)
    code.extend(bytes.fromhex("48 83 c4 48 c3"))
    code[success_jump] = done - success_jump - 1
    code[other_type_jump] = done - other_type_jump - 1
    return bytes(code)


def checksum(image, checksum_offset):
    total = 0
    for offset in range(0, len(image), 2):
        if checksum_offset <= offset < checksum_offset + 4:
            continue
        total += int.from_bytes(image[offset:offset + 2], "little")
        total = (total & 0xFFFF) + (total >> 16)
    total = (total & 0xFFFF) + (total >> 16)
    return total + len(image)


def patch_image(source):
    digest = hashlib.sha256(source).hexdigest()
    if digest != SOURCE_SHA256:
        raise ValueError(f"Unsupported or already modified client: SHA-256 {digest}. Expected {BUILD}; no output written.")
    image = bytearray(source)
    pe = struct.unpack_from("<I", image, 0x3C)[0]
    if image[:2] != b"MZ" or image[pe:pe + 4] != b"PE\0\0":
        raise ValueError("Invalid PE executable")
    coff = pe + 4
    machine, section_count = struct.unpack_from("<HH", image, coff)
    optional_size = struct.unpack_from("<H", image, coff + 16)[0]
    optional = coff + 20
    if machine != 0x8664 or struct.unpack_from("<H", image, optional)[0] != 0x20B:
        raise ValueError("Only the verified Windows x64 client is supported")
    section_alignment, file_alignment = struct.unpack_from("<II", image, optional + 32)
    header_size = struct.unpack_from("<I", image, optional + 60)[0]
    directories = optional + 112
    if any(struct.unpack_from("<II", image, directories + 4 * 8)):
        raise ValueError("Refusing to invalidate an Authenticode signature")
    table = optional + optional_size
    sections = []
    for index in range(section_count):
        offset = table + index * 40
        name, virtual_size, rva, raw_size, raw = struct.unpack_from("<8sIIII", image, offset)
        sections.append((rva, virtual_size, raw, raw_size))
        if name.rstrip(b"\0") == SECTION_NAME:
            raise ValueError("Client already has the DDS icon patch")
    new_header = table + section_count * 40
    if new_header + 40 > header_size or any(image[new_header:new_header + 40]):
        raise ValueError("No unused section-header space")

    def file_offset(rva, size=1):
        for start, _, raw, raw_size in sections:
            if start <= rva and rva + size <= start + raw_size:
                return raw + rva - start
        raise ValueError(f"RVA {rva:#x} is outside section file data")

    call_offset = file_offset(ICON_EXISTS_CALL_RVA, 5)
    if image[call_offset:call_offset + 5] != ORIGINAL_CALL:
        raise ValueError("Composite icon call site does not match the verified build")
    if max(raw + size for _, _, raw, size in sections) != len(image):
        raise ValueError("Unexpected executable overlay")

    new_rva = align(max(rva + max(size, raw_size) for rva, size, _, raw_size in sections), section_alignment)
    new_raw = align(len(image), file_alignment)
    thunk = make_lookup_thunk(new_rva)
    payload = bytearray(thunk)
    payload.extend(b"\0" * (align(len(payload), 4) - len(payload)))
    unwind_rva = new_rva + len(payload)
    # UNWIND_INFO v1: four-byte prologue, UWOP_ALLOC_SMALL for sub rsp,72.
    payload.extend(bytes.fromhex("01 04 01 00 04 82 00 00"))

    # Retain the complete original exception table and append our sorted function.
    # Windows stack unwinding must understand the thunk's stack allocation too.
    exception_rva, exception_size = struct.unpack_from("<II", image, directories + 3 * 8)
    if exception_size % 12:
        raise ValueError("Invalid runtime-function table")
    exception_offset = file_offset(exception_rva, exception_size)
    exception_table = image[exception_offset:exception_offset + exception_size]
    if struct.unpack_from("<I", exception_table, len(exception_table) - 12)[0] >= new_rva:
        raise ValueError("Runtime-function table is not ordered before the new code")
    new_exception_rva = new_rva + len(payload)
    payload.extend(exception_table)
    payload.extend(struct.pack("<III", new_rva, new_rva + len(thunk), unwind_rva))

    raw_size = align(len(payload), file_alignment)
    image.extend(b"\0" * (new_raw - len(image)))
    image.extend(payload)
    image.extend(b"\0" * (raw_size - len(payload)))
    struct.pack_into("<8sIIIIIIHHI", image, new_header,
                     SECTION_NAME, len(payload), new_rva, raw_size, new_raw,
                     0, 0, 0, 0, 0x60000020)  # executable/readable, never writable
    struct.pack_into("<H", image, coff + 2, section_count + 1)
    old_code_size = struct.unpack_from("<I", image, optional + 4)[0]
    struct.pack_into("<I", image, optional + 4, old_code_size + raw_size)
    struct.pack_into("<I", image, optional + 56, align(new_rva + len(payload), section_alignment))
    struct.pack_into("<II", image, directories + 3 * 8, new_exception_rva, exception_size + 12)
    image[call_offset:call_offset + 5] = b"\xE8" + struct.pack("<i", new_rva - ICON_EXISTS_CALL_RVA - 5)
    struct.pack_into("<I", image, optional + 64, checksum(image, optional + 64))
    return bytes(image)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True, help="Original nwmain.exe; never modified")
    parser.add_argument("--output", type=Path, help="New executable path; must not exist")
    parser.add_argument("--check-only", action="store_true", help="Validate and construct in memory without writing")
    args = parser.parse_args()
    if not args.check_only and args.output is None:
        parser.error("--output is required unless --check-only is used")
    if args.output is not None and args.output.resolve() == args.source.resolve():
        parser.error("The original executable cannot be overwritten")
    try:
        result = patch_image(args.source.read_bytes())
        if not args.check_only:
            with args.output.open("xb") as stream:
                stream.write(result)
        print(f"Verified {BUILD}; DDS-aware composite inventory lookup {'validated' if args.check_only else 'written to ' + str(args.output)}.")
        print(f"Output SHA-256: {hashlib.sha256(result).hexdigest()}")
        print("The source executable is unchanged. Clients must run the separate patched executable for this fix to apply.")
    except (OSError, ValueError) as error:
        parser.exit(1, f"{error}\n")


if __name__ == "__main__":
    main()
