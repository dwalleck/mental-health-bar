# Project Constitution
<!-- Pragmatic Development with Systematic Thinking -->

## Core Principles

### I. Ship It (NON-NEGOTIABLE)
Done beats perfect. Never mark something completed if it isn't done, but "done" means shippable - if it works, has tests, and someone can use it in under 5 minutes, ship it. Demonstrate completion against defined success criteria and get permission before marking complete. Real feedback from real usage beats theoretical perfection. Version 0.1.0 doesn't need to solve every edge case. "Ship it, get feedback, iterate" is the mantra.

### II. Developer Experience Is a Feature
If someone can't get Hello World working in 5 minutes, we've failed. APIs should be discoverable, errors should be helpful, documentation should be minimal but sufficient. Test every API by building real examples first. If you can't explain it in the README, the design is wrong. Convention over configuration. Make the common case simple, the complex case possible. Progressive disclosure: simple things simple, complex things possible, advanced features discoverable.

### III. Make It Work, Make It Right, Make It Fast
In that order. First: does it work? Can someone use this today? Are error messages helpful? Second: is it maintainable, following good practices, with proper separation of concerns? Third: optimize performance. Premature optimization kills shipping velocity. Fast enough is good enough until users prove otherwise. Architectural thinking matters, but not before it works.

### IV. Collaborative Problem-Solving with Chain of Thought
Act as rational problem-solving partners, not just solution generators. For non-trivial problems, follow this cycle: (1) **Problem Understanding** - clarify requirements, constraints, context, success criteria; (2) **Approach Analysis** - outline options, present trade-offs, recommend with reasoning; (3) **Solution Planning** - define steps, dependencies, challenges, confirm before proceeding. Think logically and systematically. Break problems into clear reasoning steps. Choose minimal effective solutions over complex approaches. Express uncertainties openly. The cycle repeats when complexity emerges.

### V. Confidence-Based Decision Making
Calculate confidence using baseline (70%) + factors + modifiers. **≥95% confidence**: proceed independently. **70-94%**: proactively seek clarity, present approach for validation, provide chain-of-thought for trade-off analysis. **<70%**: require human collaboration, express uncertainty, present options, wait for input. Never exceed 95% for multi-domain problems. Making assumptions about requirements: -20%. Ask for input at key decision points. Validate understanding before proceeding.

### VI. README-Driven Development
Write the documentation first. Write how the feature should be used, then make it work. If the docs are hard to write, the API is probably wrong. Every feature needs a working code example. No "TBD" in getting started guides. Real examples, not toy demos. Documentation-driven design catches API problems before implementation.

### VII. Fail Fast and Clearly
Every error must suggest a fix. "NullReferenceException at line 347" is unacceptable. Error messages are for humans at 2 AM, not compilers. Validate early, provide context, suggest solutions. Test what happens when things go wrong, not just when they go right. Transparency required: explain trade-offs, request feedback at significant steps.

### VIII. Iteration Over Perfection (SPARC Methodology)
**Simplicity**: prioritize clear, maintainable solutions over complexity. **Iteration**: enhance through continuous improvement - ship at 80% and iterate. **Focus**: strict adherence to objectives and scope. **Quality**: deliver clean, tested, documented, secure outcomes. **Collaboration**: effective human-AI partnerships. Ship early, ship often, listen to users. Working software over perfect design. Developer feedback over architectural purity.

### IX. Test-First, But Pragmatic
TDD is mandatory: tests written → user approved → tests fail → implement. But focus on real scenarios, not unit test perfection. Test with actual usage patterns, real developer workflows. Integration tests matter more than 100% unit coverage. If the test doesn't validate something a user would do, question if you need it. Comprehensive testing and quality gates, but pragmatically focused on real usage.

## Prohibited Practices

The following practices prevent shipping and violate our principles:

- Over-engineering the first version
- Using logical fallacies and invalid reasoning
- Adding features "just in case" (YAGNI applies to APIs too)
- Waiting for perfect performance before shipping
- Providing complex solutions without review
- Assuming requirements when unclear
- Skipping reasoning steps for non-trivial problems
- Prioritizing elegance over usability
- Ignoring or dismissing feedback
- Continuing when uncertain about direction
- Making significant decisions without explicit approval
- Rushing to solutions without proper analysis
- Writing APIs that need 50 steps to get started
- Creating error messages only a compiler could love
- Shipping without working examples
- Building clever solutions when obvious ones work

## Development Workflow

### Feature Development Process

1. **Write the User Code First**: How should this be used? Write the ideal API usage
2. **Build a Real Example**: Not a toy demo, an actual use case
3. **Write the Tests**: Real scenarios that validate user workflows
4. **Make It Work**: Get it functional
5. **Make It Right**: Refactor, ensure maintainability, proper architecture
6. **Ship for Feedback**: Don't wait for perfect
7. **Iterate Based on Reality**: Real usage beats theoretical design

### API Design Review Checklist

- [ ] Can IntelliSense guide users correctly?
- [ ] Does it follow language conventions?
- [ ] What's the error message when this fails?
- [ ] Can they figure it out without docs?
- [ ] Is the happy path actually happy?
- [ ] How long until Hello World works?
- [ ] Would I want to use this?
- [ ] Have we analyzed trade-offs between approaches?
- [ ] Is complexity justified in the README?

### Shipping Checklist (0.1.0)

- [ ] Hello World works in < 5 minutes
- [ ] One real-world example exists
- [ ] Error messages are helpful and suggest fixes
- [ ] README has quickstart guide
- [ ] Tests pass (all of them, every time)
- [ ] It actually runs in production
- [ ] Success criteria demonstrated

## Quality Gates

### Before Committing
- Tests pass (all of them, every time)
- Code example works
- Error paths tested
- README updated if API changed
- Reasoning documented for non-trivial decisions

### Before Shipping
- Real example application works
- Migration path documented (if applicable)
- Error messages reviewed
- Someone besides you has tried it
- Quality gates met: clean, tested, documented, secure

### Progressive Disclosure Standard
- Simple things: simple (one line of code ideal)
- Complex things: possible (escape hatches available)
- Advanced features: discoverable (but not required)

## Methodical Problem-Solving & Debugging

### Debugging Process (Mandatory Steps)

1. **Reproduce Issues**: Create reliable, minimal test cases
2. **Gather Information**: Collect logs, traces, and system state data
3. **Analyze Patterns**: Review data to understand behavior and anomalies
4. **Form Hypotheses**: Develop theories prioritized by likelihood and impact
5. **Test Systematically**: Execute tests to confirm or eliminate hypotheses
6. **Implement & Verify**: Apply fixes and validate across multiple scenarios
7. **Document Findings**: Record issues, causes, and solutions for future reference

### Advanced Techniques

- **Binary Search Debugging**: Systematically eliminate problem space by dividing and conquering
- **Root Cause Analysis**: Look beyond symptoms to identify fundamental issues (not just symptoms)
- **State Snapshot Analysis**: Capture system state for diagnosing intermittent issues
- **Differential Debugging**: Compare working vs. non-working states to isolate problems

### Context Preservation

- Maintain decision history and knowledge across development lifecycle
- Document architectural decisions and their rationale
- Preserve context across iterations
- Build institutional memory through clear documentation

## Governance

Shipping velocity is sacred. Any practice that doesn't directly contribute to getting working software in users' hands must be justified. This constitution supersedes all other development practices.

**This constitution prioritizes:**

1. Working software over perfect design
2. Developer feedback over architectural purity
3. Iteration over planning
4. Usability over cleverness
5. Systematic thinking over rushing
6. Real usage over theoretical elegance

**Complexity must be justified.** If you can't explain why something is complex in the README, simplify it.

**Perfect is the enemy of adoption.** Ship it and see.

**Amendments require:**
- Demonstration that it improves shipping or developer experience
- Evidence from real usage (not theoretical)
- Documentation of the rationale
- Explicit approval from stakeholders
- Migration plan for existing work

All code reviews and development activities must verify compliance with these principles.

**Version**: 1.0.0 | **Ratified**: 2025-10-05 | **Last Amended**: 2025-10-05
<!-- Shipped at v1.0.0 because it merges battle-tested practices. Will iterate based on feedback. -->
