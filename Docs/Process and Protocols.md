### Git Workflow

1. Pull the latest changes from `develop`.
2. Create a feature branch from `develop`.
3. Implement and test the feature locally.
4. Commit and push changes to the feature branch.
5. Open a pull request targeting `develop`.
6. Request peer review and address required changes.
7. Resolve merge conflicts and verify compatibility with `develop`.
8. Merge the approved PR into `develop`.
9. Test integrated functionality in `develop`.
10. Merge `develop` into the main release branch when the team
    agrees that the release is ready.

### Coding Standards

- Use CamelCase for method and variable names  
- Const variables must be all caps with an underscore between words. Additionally all const variables must be stored in a dedicated constant file  
- Functions should generally accomplish one task  
- Comment complex functions as explicitly as one can within reason  
- Write test cases for one’s own functions  
- For all non-explicitly mentioned standards reference:  
  - [https\://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

### Review Practices

- When creating a PR, provide reasons for changes or additions, and a broad overview of what you have done. Also mention if there is AI generated code in the PR description.  
- When making comments on a PR distinguish mandatory fixes from nitpicks  
- Notify the team member who opened the PR that you have reviewed it

### Work Planning

- Features will be divided into user stories.
- Stories may be divided further into development tasks.
- Work will be tracked using GitHub Issues and/or GitHub Projects.

### Communication

- Important technical or project decisions should be documented where appropriate.
- See details on [Team Working Agreement](Team%20Working%20Agreement.pdf)