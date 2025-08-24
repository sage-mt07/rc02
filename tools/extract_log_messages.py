"""Extract log messages from C# files.

The script scans the repository for calls to ILogger.Log* methods and attempts
to capture the first string literal passed as the log message. It makes a best
effort to handle multi-line statements and simple variable assignments, but it
is **not** a full C# parser. Complex cases may still produce empty messages.

Usage:
    python3 tools/extract_log_messages.py > log_messages.md

Limitations:
    - Only the first argument of Log* calls is inspected.
    - Variable assignments are searched up to five lines above the call.
    - The script does not evaluate interpolated expressions or format
      placeholders; it simply extracts the string literal.
"""

import os
import re

from collections import defaultdict

# Patterns used to map file paths to logical categories.
CATEGORY_PATTERNS = [
    (os.path.join('Kafka.Ksql.Linq.Importer'), 'Importer'),
    ('physicalTests', 'Tests'),
]

LOG_PATTERN = re.compile(r"\bLog(Debug|Information|Warning|Error|Trace|Critical)\s*\(")
LOG_CALL_RE = LOG_PATTERN


def get_category(path: str) -> str:
    """Return a category name derived from the given file path."""
    path = path.lstrip('./\\')
    for pattern, cat in CATEGORY_PATTERNS:
        if pattern in path:
            return cat

    if path.startswith('src' + os.sep):
        parts = path.split(os.sep)
        if len(parts) >= 2:
            segment = parts[1]
            if '.' in segment:  # file directly under src
                return os.path.splitext(segment)[0]
            return segment

    return 'Misc'


def get_abbr(category: str) -> str:
    """Return a three letter abbreviation for the category."""
    alnum = ''.join(c for c in category if c.isalnum())
    return alnum[:3].upper().ljust(3, 'X')

def extract_message(lines, idx, start_pos, var_name=None):
    """Return the extracted message string for the call starting at idx."""

    call_text = lines[idx][start_pos:]
    j = idx
    # Collect lines until closing parenthesis
    while ')' not in call_text and j + 1 < len(lines):
        j += 1
        call_text += lines[j]

    # look for a string literal in the call text
    literal = re.search(r'"([^"\\]*(?:\\.[^"\\]*)*)"', call_text)
    if literal:
        return literal.group(1)

    # If a variable name was provided, search previous lines for assignment
    if var_name:
        for k in range(idx - 1, max(-1, idx - 6), -1):
            segment = lines[k].strip()
            if var_name in segment and '=' in segment:
                assign = segment
                l = k
                # handle multi-line assignment ending with ';'
                while ';' not in assign and l + 1 < idx:
                    l += 1
                    assign += lines[l].strip()
                match = re.search(r'"([^"\\]*(?:\\.[^"\\]*)*)"', assign)
                if match:
                    return match.group(1)
    # fallback to the variable name if nothing could be resolved
    return f'<{var_name}>' if var_name else ''


def parse_file(path):
    entries = []
    with open(path, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
    for i, line in enumerate(lines):
        m = LOG_CALL_RE.search(line)
        if not m:
            continue
        severity = m.group(1)
        after_pos = m.end()
        # Capture the first argument token
        after = line[after_pos:]
        arg_match = re.match(r"\s*([A-Za-z_][A-Za-z0-9_]*|\")", after)
        var_name = None
        if arg_match and not arg_match.group(0).startswith('"'):
            var_name = arg_match.group(1)
        message = extract_message(lines, i, after_pos, var_name)
        entries.append((path.lstrip('./'), i + 1, severity, message))
    return entries


entries = []
for root, dirs, files in os.walk('.'):
    if '.git' in root:
        continue
    for fname in files:
        if fname.endswith('.cs'):
            file_entries = parse_file(os.path.join(root, fname))
            for e in file_entries:
                category = get_category(e[0])
                entries.append((e[0], e[1], e[2], e[3], category))

entries.sort()

category_counts = defaultdict(int)

print('| File | Line | Severity | Message | Category | LogID |')
print('|------|------|----------|---------|----------|-------|')
for path, line_no, severity, message, category in entries:
    category_counts[category] += 1
    log_id = f"{get_abbr(category)}-{category_counts[category]:03d}"
    print(f'| {path} | {line_no} | {severity} | {message} | {category} | {log_id} |')
