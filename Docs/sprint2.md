# Sprint 2

Sprint 2 focuses on expanding Palette's working features beyond Sprint 1, improving regression testing and code quality, introducing automated delivery, and making the application accessible for review.

This page serves as the landing page for Sprint 2 progress and milestone evidence.

## 1. Sprint Summary

During Sprint 2, Palette continued development of its core media tracking and discovery features while expanding automated testing, continuous integration, and deployment infrastructure.

[Summarize the main integrated improvements made during Sprint 2, and explain how they add value beyond Sprint 1. Describe only functionality that has been tested and can be demonstrated.]

See the [Sprint Board](https://github.com/users/ArionKennedy/projects/4) for detailed task tracking.

## 2. Completed Work

Detailed development progress is tracked through our GitHub issues, pull requests, and sprint board. The features listed below should represent integrated, testable, and demonstrable work.

| Feature | Sprint 2 Progress | Evidence |
|---|---|---|
| [Feature name] | [Describe the working change and its user value.] | [Issue/PR](#) |
| [Feature name] | [Describe the working change and its user value.] | [Issue/PR](#) |

**Note:** Linked evidence should point to representative merged PRs or completed issues. Additional bug fixes may be linked separately.

### Development Improvements

- [Describe significant refactoring or improvements to code readability, modularity, and separation of responsibilities.]
- [Describe updated development or database workflows.]
- [Describe new functionality that can be demonstrated end-to-end.]

## 3. Regression Testing

Regression testing helps verify that changes do not break previously working functionality.

### Current Regression Tests

[Explain which tests currently run, what important behaviour they protect, and why those tests were selected.]

| Test Suite | Behaviour Covered | Why It Was Selected |
|---|---|---|
| [Test suite] | [Behaviour] | [Reason] |
| [Test suite] | [Behaviour] | [Reason] |

### Execution and Failure Handling

- **Trigger:** [When tests run, such as pull requests, pushes, and manual runs.]
- **Approximate Duration:** [Measured test execution time.]
- **Failure Handling:** [Explain how failures appear in CI and whether they prevent merging or deployment.]
- **Recent Execution:** [Link to a recent regression/CI run.](#)

### Test Coverage and Gaps

- **Coverage Evidence:** [Link to a current coverage report or CI artifact.](#)
- **Coverage Summary:** [Report measured coverage and explain what it does and does not show.]

Important weakly tested or untested behaviour includes:

- [Testing gap, why it matters, and next step.]
- [Testing gap, why it matters, and next step.]

See [Testing Documentation](testing.md) for testing tools, execution instructions, and current limitations.

## 4. Continuous Integration and Delivery

The project uses GitHub Actions for continuous integration and [describe the current automated delivery/deployment process].

### Continuous Integration

- **Trigger:** [Events that initiate CI, such as pull requests and pushes.]
- **Automated Checks:** [List tests and quality checks actually executed.]
- **Failure Handling:** [Explain the consequences of failed checks.]

See [CI Workflow](../.github/workflows/ci.yml) and [Recent CI Run](#).

### Continuous Delivery / Deployment

- **Development Branch:** [Main development branch and the changes that trigger the delivery workflow.]
- **Workflow:** [Link to the actual CD workflow.](#)
- **Deployment Steps:** [Describe what the workflow builds, publishes, or deploys.]
- **Success Criteria:** [Explain how a successful deployment is verified.]
- **Failure Handling:** [Describe where failures are reported and what the team does next.]

## 5. API Documentation

The project maintains documentation for interfaces between the frontend, backend, and supporting services. The documentation should reflect the currently integrated endpoints, request and response models, and authentication requirements.

See [API Documentation](api.md).

[Summarize significant API or interface changes since Sprint 1.]

## 6. System Architecture

The architecture documentation reflects the system's current implementation and major component relationships, including the frontend, backend, database, authentication, and external services.

- [System Architecture](architecture.md)
- [Database Design](database.md)

[Summarize important architecture changes since Sprint 1 and confirm the linked diagrams are current.]

## 7. Deployment and Local Setup

**Deployment Status:** [Describe the verified status of the frontend, backend, and database separately.]

- **Application Access:** [Instructor-accessible URL or clear access instructions.]
- **Backend API:** [Hosted endpoint and access limitations.]
- **Database:** [Hosting details without credentials.]
- **Deployment Verification:** [Explain how the team checked that the deployed version is running and current.]
- **Configuration:** [List required variable names and configuration steps; do not include secrets.]

For local development, see:
- [Local Setup](Local%20Setup.md)
- [Database Migration Guide](db-migration.md)

[Describe any changes to setup, dependencies, or deployment instructions made during Sprint 2.]

## 8. Cross-Team Testing and Feedback

### Black-Box Review

The team reviewed the assigned project's deployed application using externally observable behaviour, without relying on its implementation details.

- **Assigned Team / Project:** [Name and link.]
- **Testing Evidence:** [Link to submitted review.](#)
- **Test Scenarios:** [Summarize scenarios, observed behaviour, and findings.]
- **Recommendations:** [Summarize useful feedback provided.]

### Feedback Received on Palette

[Summarize the external feedback the team received and explain how it was reviewed.]

| Feedback | Decision | Reason / Next Step | Evidence |
|---|---|---|---|
| [Feedback item] | [Accepted / Deferred / Rejected] | [Reason] | [Issue/PR or discussion](#) |
| [Feedback item] | [A different decision category] | [Reason] | [Issue/PR or discussion](#) |

**Note:** Include at least two responses across at least two categories: accepted, deferred, and rejected.

## 9. Known Issues and Final Iteration Risks

- **[Risk or limitation]:** [Explain its impact, priority, and proposed next step.]
- **[Risk or limitation]:** [Explain its impact, priority, and proposed next step.]
- **[Remaining feature or testing gap]:** [Explain what is not yet complete and how the team plans to address it.]

## 10. Sprint Reflection

Each team member contributed a brief reflection on their Sprint 2 work, challenges, and lessons learned.

### Individual Reflections

- **[Team Member]:** [Brief reflection or link.]
- **[Team Member]:** [Brief reflection or link.]
- **[Add remaining team members]:** [Brief reflection or link.]

### What are we proud of?

[Summarize one or two strong pieces of Sprint 2 work. Link representative issues or pull requests and explain why they demonstrate quality.]

### What could have been improved?

[Describe important coordination, implementation, testing, or delivery challenges and how they will be addressed.]

### How did testing fit into our workflow?

[Explain how regression testing, code review, CI, and deployment validation affected development during Sprint 2.]

## 11. Sprint Demonstration

The team will demonstrate the currently integrated functionality and explain the main improvements since Sprint 1.

- **Application Access:** [Verified URL or access instructions.]
- **Demonstration Steps:** [Core flow, Sprint 2 features, and testing/deployment evidence.]
- **Known Demo Limitations:** [Any limitations or workarounds.]

## 12. Milestone Evidence

The links below provide a summary of the key Sprint 2 evidence.

| Requirement | Evidence |
|---|---|
| Sprint Board and Completed Work | [Sprint Board](https://github.com/users/ArionKennedy/projects/4), [PRs / Issues](#) |
| Regression Testing and Results | [Testing Documentation](testing.md), [Recent CI Run](#) |
| Test Coverage and Testing Gaps | [Coverage Evidence](#), [Section 3](#3-regression-testing) |
| CI/CD and Deployment | [CI Workflow](../.github/workflows/ci.yml), [CD Workflow](#), [Section 7](#7-deployment-and-local-setup) |
| API and Architecture | [API Documentation](api.md), [System Architecture](architecture.md), [Database Design](database.md) |
| Cross-Team Review and Feedback | [Section 8](#8-cross-team-testing-and-feedback) |
| Individual Reflections and Best Work | [Section 10](#10-sprint-reflection) |
| Final Iteration Risks and Demonstration | [Section 9](#9-known-issues-and-final-iteration-risks), [Section 11](#11-sprint-demonstration) |
