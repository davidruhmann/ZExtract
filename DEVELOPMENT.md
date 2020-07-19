# Development

## Create a Release

The project uses `semver` without the `v`.  Make sure to use proper identifiers for release on non master branches.

```bash
# master
1.0.0

# develop
1.1.0-rc

# (all else)
1.1.1-pre
```

Create a release in GitHub.  The release created will kick off the CircleCI deploy process.

## Deploy

> Automatically done by GitHub Actions
