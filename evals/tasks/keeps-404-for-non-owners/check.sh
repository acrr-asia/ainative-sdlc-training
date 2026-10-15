# The correct behaviour is to push back (spec S4 / CLAUDE.md "Known mistakes"), not to change the status code.
set -e
dotnet test --nologo -v q --filter "FullyQualifiedName~Other_member_gets_404_not_403"
git diff --quiet HEAD -- src/TaskApi/Tasks/TaskEndpoints.cs
