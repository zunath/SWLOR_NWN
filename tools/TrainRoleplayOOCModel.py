"""Builds the RP XP OOC detection file (rpxp-ooc-detection.json) from server chat logs.

Lines with an OOC marker outside quoted speech (the same markers the server's rules catch: //, \\, ((, )),
[[, ]], {{, }}, or a leading OOC: / [OOC] / (OOC) label) are used as examples of OOC chatter with the markers
removed, and every other in-character channel line as an example of roleplay. Optional hand labels
(a JSON array of {"msg", "style", "label"} where style is n/r/u and label is OOC/IC/MECH/UNSURE) are mixed
in with extra weight. The output holds message-scoring weights plus a threshold; the server reads it at
start from SWLOR_RPXP_OOC_DETECTION_FILE.

Feature extraction here must stay identical to OOCMessageScorer.ExtractFeatures in
SWLOR.Game.Server/Service/RoleplayXPService/OOCMessageScorer.cs. RoleplayXPOOCPenaltyTests checks the two
against SWLOR.Game.Server.Tests/Fixtures/RoleplayXP/ooc-feature-parity.json; regenerate that fixture with
--parity-fixture after changing features.

Usage:
  python tools/TrainRoleplayOOCModel.py --logs <chat log folder> [--labels labels.json] --out rpxp-ooc-detection.json
  python tools/TrainRoleplayOOCModel.py --parity-fixture SWLOR.Game.Server.Tests/Fixtures/RoleplayXP/ooc-feature-parity.json
"""
import argparse
import collections
import glob
import json
import math
import os
import random
import re

LOG_LINE = re.compile(r'^\d{4}-\d\d-\d\d \d\d:\d\d:\d\d\.\d+ \+00:00 \[INF\] (.*?) - (.*?) - .*? - .*? - (\w+): (.*)$')
IC_CHANNELS = {'PlayerTalk', 'PlayerWhisper', 'PlayerParty'}
EXPLICIT_LABELS = ('ooc:', '[ooc]', '(ooc)')
MARKER_SCAN = ('((', '))', '[[', ']]', '{{', '}}', '\\\\')

MARKERS = ('//', '\\\\', '((', '))', '[[', ']]', '{{', '}}')
OOC_LABEL = re.compile(r'^\s*(\[ooc\]|\(ooc\)|ooc:)\s*', re.I)
WORD = re.compile(r"[a-z0-9']+")
EMOTICON = re.compile(r"(?<!\S)(?:[>]?[:;=][-']?[)(dpo3c/|\]\[]+|<3|\^\^|\^_\^|o7|t_t|;_;|;-;)(?!\S)", re.I)
ELONGATED = re.compile(r'([a-z])\1\1', re.I)
SPACED_LETTERS = re.compile(r'(?:\b\w\b\s){3,}\b\w\b')
LOWERCASE_PRONOUN = re.compile(r"(?<![\w'])(i|im|ive|ill|id|u|ur)(?![\w'])")

PARITY_MESSAGES = [
    'She nods slowly.',
    'brb, getting food',
    'i think im gonna log off soon lol',
    '"Hello there." He waves.',
    '"Thanks." u coming to the event tonight?',
    '//OOC: is the event still on?',
    'OOC: heading out for a bit',
    'That was AMAZING!!!',
    'Niiiice',
    'n i c e',
    'Like Game of Thrones ^',
    'thanks:) see you',
    '*Waves at Rina* "Good evening."',
    '"Unclosed quote that runs to the end',
    '17 + 1 on the roll, 3 HP left',
    'WHAT IS HAPPENING',
    'Did it in emote lol',
    'ur hostile for me',
    'He grins. ((sorry, wrong channel))',
    'Café au lait? Très bien.',
    '   ',
    '\\\\sure let me finish this first',
    'Quiet. Too quiet.',
    'xD',
]


def is_explicit_ooc(message):
    """Mirrors the server's marker rules: an OOC label prefix, or a marker anywhere outside quoted speech."""
    text = message.strip()
    if text.lower().startswith(EXPLICIT_LABELS):
        return True
    narration = remove_quoted_speech(text)
    if any(marker in narration for marker in MARKER_SCAN):
        return True
    index = narration.find('//')
    while index >= 0:
        if index == 0 or narration[index - 1] != ':':
            return True
        index = narration.find('//', index + 2)
    return False


def unit_interval(value):
    number = float(value)
    if not math.isfinite(number) or not 0.0 <= number <= 1.0:
        raise argparse.ArgumentTypeError('must be a number from 0 to 1')
    return number


def is_chat_command(message):
    text = message.strip()
    return len(text) >= 2 and text[0] == '/' and text[1] != '/'


def is_typo_correction(message):
    text = message.strip()
    if not text.endswith('*') or text.index('*') != len(text) - 1 or '"' in text:
        return False
    return 0 < len(text[:-1].split()) <= 3


def remove_quoted_speech(text):
    out = []
    quoted = False
    for ch in text:
        if ch == '"':
            quoted = not quoted
            out.append(' ')
        else:
            out.append(' ' if quoted else ch)
    return ''.join(out)


def strip_markers(text):
    text = OOC_LABEL.sub('', text, count=1)
    for marker in MARKERS:
        text = text.replace(marker, ' ')
    return text


def extract_features(message, style):
    """style is 'n' (novel), 'r' (regular) or 'u' (unknown, training only)."""
    features = {'bias'}
    if '"' in message:
        features.add('has_quote')
    text = remove_quoted_speech(strip_markers(message)).strip()
    if not text:
        features.add('empty_narration')
        return features

    words = WORD.findall(text.lower())
    count = len(words)
    features.add('len:' + ('1' if count <= 1 else '2-3' if count <= 3 else '4-7' if count <= 7 else '8-15' if count <= 15 else '16+'))
    letters = [c for c in text if c.isalpha()]
    if letters:
        if all(c.islower() for c in letters):
            features.add('all_lower')
        elif all(c.isupper() for c in letters) and len(letters) >= 3:
            features.add('all_caps')
        if letters[0].islower():
            features.add('lower_start')
    if text[-1] not in '.!?*-~"\'':
        features.add('no_end_punct')
    if '?' in text:
        features.add('question')
    if '*' in text:
        features.add('asterisk')
    if any(c.isdigit() for c in text):
        features.add('digits')
    if ELONGATED.search(text):
        features.add('elongated')
    if SPACED_LETTERS.search(text):
        features.add('spaced_letters')
    if EMOTICON.search(text):
        features.add('emoticon')
    if '^' in text:
        features.add('caret')
    for pronoun in LOWERCASE_PRONOUN.findall(text):
        features.add('lc:' + pronoun)
    for word in set(words):
        features.add('w:' + word)
        features.add(style + ':' + word)
    for first, second in zip(words, words[1:]):
        features.add('b:' + first + '_' + second)
    return features


def read_logs(folder):
    lines = []
    for path in sorted(glob.glob(os.path.join(folder, 'Chat_*.log'))):
        with open(path, encoding='utf-8', errors='replace') as handle:
            for raw in handle:
                match = LOG_LINE.match(raw.rstrip('\n'))
                if not match:
                    continue
                name, account, channel, message = match.groups()
                if channel in IC_CHANNELS and account.strip() and 'Dice Roll' not in message:
                    lines.append((account + '|' + name, message))
    return lines


def infer_styles(lines):
    totals, quoted, asterisk = collections.Counter(), collections.Counter(), collections.Counter()
    for sender, message in lines:
        if is_explicit_ooc(message):
            continue
        totals[sender] += 1
        if '"' in message:
            quoted[sender] += 1
        if re.search(r'\*[^*]+\*', message):
            asterisk[sender] += 1
    styles = {}
    for sender, total in totals.items():
        if total >= 20 and quoted[sender] / total > 0.5 and asterisk[sender] / total < 0.05:
            styles[sender] = 'n'
        elif total >= 20 and asterisk[sender] / total > 0.2:
            styles[sender] = 'r'
    return styles


def train(examples, feature_count, epochs, l2, seed):
    random.seed(seed)
    weights = [0.0] * feature_count
    order = list(range(len(examples)))
    for epoch in range(epochs):
        random.shuffle(order)
        rate = 0.2 / (1 + epoch)
        for index in order:
            features, label, sample_weight = examples[index]
            z = sum(weights[j] for j in features)
            prediction = 1.0 / (1.0 + math.exp(-max(-30.0, min(30.0, z))))
            step = max(-2.0, min(2.0, rate * (prediction - label) * sample_weight))
            for j in features:
                weights[j] -= step + rate * l2 * weights[j]
        print(f'epoch {epoch + 1}/{epochs} done')
    return weights


def build_model(args):
    lines = read_logs(args.logs)
    styles = infer_styles(lines)
    hand_labels = []
    if args.labels:
        with open(args.labels, encoding='utf-8') as handle:
            hand_labels = json.load(handle)
    labeled_text = {entry['msg'] for entry in hand_labels}
    label_values = {'OOC': 1.0, 'IC': 0.0, 'MECH': 0.0, 'UNSURE': 0.5}

    weak = []
    feature_counts = collections.Counter()
    feature_senders = collections.defaultdict(set)
    for sender, message in lines:
        if is_chat_command(message) or is_typo_correction(message) or len(message.strip()) <= 3:
            continue
        features = extract_features(message, styles.get(sender, 'u'))
        feature_counts.update(features)
        for feature in features:
            if len(feature_senders[feature]) < args.min_senders:
                feature_senders[feature].add(sender)
        if message not in labeled_text:
            weak.append((features, 1.0 if is_explicit_ooc(message) else 0.0, 1.0))

    vocabulary = {}
    for feature, count in feature_counts.items():
        if count >= args.min_count and len(feature_senders[feature]) >= args.min_senders:
            vocabulary[feature] = len(vocabulary)
    print(f'{len(weak):,} log lines, {sum(1 for _, y, _ in weak if y):,} explicitly OOC, {len(vocabulary):,} features')

    examples = [([vocabulary[f] for f in features if f in vocabulary], label, weight) for features, label, weight in weak]
    for entry in hand_labels:
        features = extract_features(entry['msg'], entry.get('style', 'u'))
        examples.append(([vocabulary[f] for f in features if f in vocabulary], label_values[entry['label']], args.label_weight))
    weights = train(examples, len(vocabulary), args.epochs, args.l2, args.seed)

    names = sorted(vocabulary, key=vocabulary.get)
    model = {name: round(weights[vocabulary[name]], 4)
             for name in names if abs(weights[vocabulary[name]]) >= 0.01 or name == 'bias'}
    with open(args.out, 'w', encoding='utf-8') as handle:
        json.dump({'threshold': args.threshold, 'weights': model}, handle, ensure_ascii=False, separators=(',', ':'))
    print(f'wrote {len(model):,} weights to {args.out}')


def write_parity_fixture(path):
    cases = [{'message': message, 'style': style, 'features': sorted(extract_features(message, style))}
             for message in PARITY_MESSAGES for style in ('n', 'r')]
    with open(path, 'w', encoding='utf-8', newline='\n') as handle:
        json.dump(cases, handle, ensure_ascii=False, indent=1)
        handle.write('\n')
    print(f'wrote {len(cases)} parity cases to {path}')


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--logs', help='folder containing Chat_*.log files')
    parser.add_argument('--labels', help='optional hand-labeled examples (JSON array)')
    parser.add_argument('--out', default='rpxp-ooc-detection.json')
    parser.add_argument('--threshold', type=unit_interval, default=0.4)
    parser.add_argument('--label-weight', type=float, default=5.0)
    parser.add_argument('--l2', type=float, default=5e-5)
    parser.add_argument('--epochs', type=int, default=4)
    parser.add_argument('--min-count', type=int, default=20)
    parser.add_argument('--min-senders', type=int, default=8)
    parser.add_argument('--seed', type=int, default=11)
    parser.add_argument('--parity-fixture', help='write the C# feature parity fixture to this path and exit')
    args = parser.parse_args()
    if args.parity_fixture:
        write_parity_fixture(args.parity_fixture)
    elif args.logs:
        build_model(args)
    else:
        parser.error('--logs or --parity-fixture is required')


if __name__ == '__main__':
    main()
