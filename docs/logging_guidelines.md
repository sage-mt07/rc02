# Logging Guidelines

This project uses structured logging throughout the code base. The following conventions keep log output predictable and easy to grep:

- **Sentence case:** Start each message with a capital letter.
- **No emoji or decorative punctuation.**
- **One sentence per message.** Do not end with a period.
- **Success messages:** `Action succeeded: {Detail}`
- **Failure messages:** `Failed to {action}: {Reason}`
- **Placeholders** use PascalCase inside braces (e.g., `{TopicName}`).
- Keep wording concise; avoid abbreviations like "SKIP".

These rules apply to all log calls in `src/`.
