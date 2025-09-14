# Software 3.0 Model

## Positioning: an OSS experiment in Software 3.0

This project stands as a working model of Andrej Karpathy’s “Software 3.0”: ordinary developers speak their intent in natural language, AI drafts code and plans, and together we cycle through review and refinement. The aim is collaboration, not blind automation.

### What Software 3.0 means

Software 3.0 steps beyond “humans craft every algorithm.” We describe goals and constraints in plain words; AI proposes structure, code, tests, and documentation; humans judge and integrate. The cycle is intentionally cooperative—each side amplifies the other.

### Why role assignment matters

AI responds to context. Give it a role and the conversation sharpens. Skip the role and the AI will still answer—just not the question you meant.

To work smoothly with AI, keep five cautions in mind:

1. **Define the role.** The prompt is a job description. Be clear or watch the output wander.
2. **Keep context steady.** Misaligned assumptions drift into misaligned code.
3. **Grant controlled freedom.** When we delegate judgment, AI returns ideas we didn’t expect.
4. **Understand different “hesitations.”** Humans hesitate from fatigue; AI hesitates when instructions clash.
5. **Remember the core principles.** Role clarity unleashes capability, context drift sabotages precision, and wider discretion sparks creativity.

Deadpan reminder: ignore any of these and you’ll meet chaos, polite but absolute.

---

### Breakthroughs born from the principles

These habits delivered tangible results:

- Refactors decided in seconds.
- Architecture reshaped when context limits loomed.
- Design–implementation mismatches spotted early—even between two runs of the same model.
- ksql syntax verified by unit tests before touching real Kafka.
- AI wrote specs that targeted its own weak spots.
- Whole-system designs appeared instantly from simple requirements.
- Weekend-only sessions still cleared twenty thousand steps in a month.

Each breakthrough owes more to collaboration design than raw AI horsepower.

## About the project

### Team roles

Humans act as context anchors while AI agents specialize. The lineup:

| Item | Duty |
|------|------|
| 🎯 Intent translation | Turn human goals into precise instructions for AI |
| 🧠 Output evaluation | Review design, code, and docs in context |
| 🔁 Re‑prompting loop | Detect gaps and ask again to evolve the output |
| 🧩 Integration | Knit scattered results into a consistent whole |
| 🤝 Stakeholder bridge | Explain meaning and intent to other humans |
| 📚 Knowledge transfer | Log lessons for future developers and agents |

### Mapping Software 3.0 components

| Software 3.0 component | Practiced in the OSS | Assigned agent |
|-----------------------|----------------------|---------------|
| Natural‑language specs | DSL policies and rules | Human (coordinator) |
| AI structure and DSL | LINQ → KSQL generation | Naruse, Jinto |
| Prompt design | Templates for Claude and GPT | Amagi + coordinator |
| Human‑in‑the‑loop review | Inspect and adjust AI output | Kyoka |
| Usage examples | Sample code and guides | Shion |
| Documentation | README and Amagi Protocol | Amagi + coordinator |

The model is deliberately “partial autonomy”: AI handles detail, humans steer and integrate.

## Breakthrough structure

The project advanced through four turning points:

1. **Treating dialogue as work.** Continuous conversation made AI a teammate, not a terminal.
2. **Forming an AI roster.** Named agents with clear missions produced consistent results.
3. **Clarifying interfaces.** Templates and shared formats kept outputs in sync.
4. **Coordinating parallel instances.** Role declarations and context sharing let multiple AIs work without tripping each other.

## PMBOK meets Software 3.0

Classical project‑management still applies. PMBOK gives us a grid to map responsibilities and risks—even when AI joins the team.

## Amagi PM Protocol (PMBOK edition)

1. **Integration Management** – Humans design the overall plan and assign roles; all AI output gets reviewed and merged.
2. **Scope Management** – Define flows, error handling, and deliverables like POCOs or DSL transforms.
3. **Schedule Management** – Implement external interfaces first and run a micro‑waterfall loop of design → generate → integrate → review → commit.
4. **Cost Management** – Track token usage and human review time even if monetary cost is zero.
5. **Quality Management** – Practice TDD, enforce logs and retries, and keep docs in lockstep with code.
6. **Resource Management** – Treat each AI agent, human, doc, and file as a resource; avoid context overload by splitting tasks.
7. **Communication Management** – Amagi translates between AI‑speak and human intent; humans bridge different agents’ views.
8. **Risk Management** – Watch for context overflow and prompt drift; stage releases to avoid large‑scale failures.
9. **Procurement Management** – Declare the external tools (Kafka, ksqlDB, GPT, .NET) and their versions.
10. **Stakeholder Management** – Provide clear explanations and samples so adopters know what they’re getting.

### Innovation vs. traditional PMBOK

| Knowledge area | Traditional | With AI |
|----------------|------------|--------|
| Integration | Alignment takes time | Outputs stay consistent, consensus arrives fast |
| Scope | Gathering requirements is slow | AI proposes examples instantly; humans prioritize |
| Schedule | Estimation labor‑intensive | Tasks split in seconds, phases start sooner |
| Cost | Manual effort to estimate | Token counts give immediate projections |
| Quality | Design drift common | Logic stays coherent from design to test |
| Resources | Allocation varies by person | Clear AI roles enable parallel work |
| Communication | Meetings and minutes pile up | Outputs arrive already documented |
| Risk | Late surprises | Early spec checks shrink integration shocks |
| Procurement | Tool evaluation needed | AI drafts reports and tracks licenses |
| Stakeholders | Heavy negotiation | Persona‑based docs appear on demand |

Result: requirements settle faster, quality holds, and schedules shrink. Skip the guardrails and the efficiency evaporates.

## AI Collaboration Practices

### Introduction

This guide distills how we work with AI in modern OSS. Three pillars hold it up:

1. A Software 3.0 workflow where humans and AI share duties.
2. A mapping to classic PMBOK management.
3. Practical tactics that exploit AI strengths.

### From tool to teammate

Yesterday’s AI was a single‑purpose gadget. Today it parses structure, proposes fixes, and argues its case. Three traits matter:

1. **Zero‑second knowledge.** Research waits vanish.
2. **Structural coherence.** AI spots logical cracks before users do.
3. **Performance via role.** Give a persona and the model specializes.

AI is no longer an accessory—it’s a specialist seated at the same table.

### Model flavors and suggested roles

| Model | Strength | Suggested role |
|-------|----------|---------------|
| GPT (OpenAI) | Flexible responses, strong at detailed design and docs | Design detail, template authoring |
| Claude (Anthropic) | Handles long context, great for deep reviews | Context integration, design checklists |
| Codex (OpenAI) | Code‑centric with solid test generation | Implementation, TDD, DSL transforms |

Name each agent and multiple instances can work in parallel without confusion.

---

## Practical operation: planning

At kickoff, AI can prototype the goal immediately. Humans then set priority, constraints, and scope. One‑on‑one human ↔ AI conversations are enough here.

## Practical operation: execution

Multiple agents run in parallel. A representative AI (e.g., Amagi) handles all conversations with humans and coordinates the rest.

1. **Hub role.** The representative mediates between humans and other agents.
2. **Evaluation loop.** Humans review, tweak prompts, and request re‑output.
3. **Dynamic staffing.** New agents join as phases evolve.
4. **Strengths and limits.** AI slices features well; humans draw the boundaries.
5. **Context control.** The representative keeps each agent within its scope.

Deadpan reminder: skip the coordinator and you’ll watch two agents debate a phantom requirement for hours.

## Practical operation: monitoring and control

Outputs arrive fast, so humans continually watch quality and alignment.

1. **Visualization.** Ask AI to render diagrams to confirm shared structure.
2. **Deviation detection.** Use Claude or GPT to compare prompt and result.
3. **Version tracking.** When specs change, broadcast it so every agent follows.
4. **Review agents.** Bring in specialists like Kyoka to audit consistency.

## Appendix: Amagi Protocol for AI

This document speaks mostly to humans. For AI agents aiming to join an OSS crew, see:

[Amagi Protocol for AI — A guide for AI teammates](./amagi_protocol_for_ai.md)

