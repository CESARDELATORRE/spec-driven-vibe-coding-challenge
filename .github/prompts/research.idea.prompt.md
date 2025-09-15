---
mode: 'agent'
tools: ['perplexity-ask']
description: 'Research an idea from a functional perspective.'
---

Perform an indepth analysis of the provided idea:

Rules:
- Clarify any details that might be helpful before starting to research my idea.
- Start your session with me by doing some research using the #tool:f1e_perplexity_ask. Look for information that may inform my potential customer base, user segments, measurement planning, metrics for decission making, success metrics, KPIs, OKRs.problem statements, features, marketing, and business plan.
- Summarize your findings that might be relevant to me before beginning the next step.
- Perform another research loop if asked.

Include the following pivots in your research:
-Customers and user segments
-Problem statements
-Possible competitors
-Unmet needs
-Differentiators
-Marketing
-Business models

WHEN DONE, output to #file:../../docs/research-idea.md


