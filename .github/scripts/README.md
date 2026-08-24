# Package Validation Scripts

`verify-release-package.sh` and `verify-release-package.cmd` are equivalent host
wrappers for the same repository package-validation contract.

Both wrappers require:

```text
<artifact-directory> <Staging|Release>
```

For pull-request/development validation:

```text
.github\scripts\verify-release-package.cmd artifacts Staging
bash .github/scripts/verify-release-package.sh artifacts Staging
```

For final main-branch release validation:

```text
.github\scripts\verify-release-package.cmd artifacts Release
bash .github/scripts/verify-release-package.sh artifacts Release
```

The scripts reject any configuration other than `Staging` or `Release`, and the
selected configuration controls maintenance tools, API-snapshot build-output
paths, the package verifier, both fresh-package consumers, and the
non-interactive repository sample.

- Use `verify-release-package.sh` on Bash-capable hosts and in Ubuntu GitHub
  Actions package-validation jobs.
- Use `verify-release-package.cmd` from Windows Command Prompt; Bash and Python
  are not required.
