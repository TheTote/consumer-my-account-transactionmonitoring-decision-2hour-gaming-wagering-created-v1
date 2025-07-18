# Tote.TransactionMonitoringDecision2HourGamingWageringConsumer

[![Quality Gate Status](https://sonarqube.services.tote.co.uk/api/project_badges/measure?project=Tote.TransactionMonitoringDecision2HourGamingWageringConsumer&metric=alert_status&token=sqb_a195eea46ae81050e4b673689f2809b6931461b5)](<https://sonarqube.services.tote.co.uk/dashboard?id=Tote.TransactionMonitoringDecision2HourGamingWageringConsumer>) [![codecov](https://codecov.io/gh/TheTote/Tote.TransactionMonitoringDecision2HourGamingWageringConsumer/graph/badge.svg?token=w9grpAmr2N)](<https://codecov.io/gh/TheTote/Tote.TransactionMonitoringDecision2HourGamingWageringConsumer>)

## Folder Structure Standards

Tote standards for folder structure within APIs must be followed, and can be found here: [Folder Structure Standards](https://thetote.atlassian.net/wiki/spaces/EN/pages/2906849281/Tote+Clean+Architecture#Folder-Structure-and-Contents-within-layers)

___
## Building Docker Container

This project used BuildKit to build the Docker container. It does so by using remote bake definitions to help maintain consistency across repositories. To build the Docker container, run the following command:

```bash
export Nuget_ToteFeedUserName=ci
export Nuget_ToteFeedPassword=<YOUR_GITHUB_PAT>
export BUILDX_BAKE_GIT_AUTH_TOKEN=<YOUR_GITHUB_PAT>
docker buildx bake \
  -f cwd://docker-bake.hcl \
  -f docker/buildx-dotnet/bake.hcl \
  --set "*.platform=linux/amd64" \
  "https://github.com/thetote/workflows.git#main" \
  application-build-github-all-platforms
```

### Local Bake Definition

The file `docker-bake.hcl` is a local bake definition that is used to build the Docker container. It is used to set application specific values which are then used in the remote bake definition.

## GitHub Workflows

This repository uses GitHub Actions to automate the build and deployment process. The workflows are defined in the `.github/workflows` directory. The workflows are defined in the following files:

- `code-quality.yml`: This workflow will build, run tests, scan with SonarQube, and upload code coverage. It will execute on pushes to a branch with an open pull request.
- `release.yml`: This will determine the next version number based on [conventional commit](https://www.conventionalcommits.org/en/v1.0.0/#summary) messages. If a new release is required it will then build a multi platform
  docker image and push it to the GitHub Container Registry. It will then tag the repository and create a GitHub release. It will execute on all pushes to the main branch. If you require a pre-release build in order to deploy for testing you can choose to manually execute the workflow from the GitHub Actions UI on your feature branch. A feature branch is deemed as a branch with a prefix of `feature/`.
- `publish-eng-docs.yml`: This will publish the documentation to the [Engineering Documentation](https://docs.tote.engineering/) site. It will execute on pushes to the main branch.

Workflows and actions are located in the `github.com/thetote/workflows` repository. This enables them to be shared amongst other repositories.
