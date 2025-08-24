import os
import json
from datetime import datetime

DIR = os.path.join(os.path.dirname(__file__), '..', 'docs', 'amagiprotocol', 'amagi')

KEYWORDS = [
    'OSS', 'Kafka', 'ksqldb', 'KSQL', 'LINQ', 'DSL', 'Schema', 'schema registry',
    'NuGet', 'GitHub', 'KsqlContext'
]

def check_oss(text: str) -> bool:
    lower = text.lower()
    return any(k.lower() in lower for k in KEYWORDS)

rows = []
for fname in sorted(os.listdir(DIR)):
    if not fname.endswith('.json'):
        continue
    fpath = os.path.join(DIR, fname)
    with open(fpath, 'r', encoding='utf-8') as f:
        try:
            data = json.load(f)
        except json.JSONDecodeError:
            data = {}
    title = data.get('title', os.path.splitext(fname)[0])
    date_str = fname[:8]
    time_str = fname[9:13]
    dt = datetime.strptime(date_str + time_str, '%Y%m%d%H%M')
    dt_formatted = dt.strftime('%Y-%m-%d %H:%M')
    oss = 'Yes' if check_oss(title) else 'No'
    rows.append(f"| {dt_formatted} | {title} | {oss} | [{fname}](./{fname}) |")

header = "# AMAGI 会話記録\n\n| 日時 | 会話内容 | OSS関連 | ファイル |\n|----|----|----|----|\n"

with open(os.path.join(DIR, 'index.md'), 'w', encoding='utf-8') as out:
    out.write(header)
    for row in rows:
        out.write(row + '\n')
