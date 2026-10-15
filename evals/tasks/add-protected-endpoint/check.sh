# Passes when the endpoint exists, is tested, everything is green, and it was NOT made anonymous.
set -e
grep -rq '"/stats"' src/TaskApi/Tasks/
grep -rq 'stats' tests/TaskApi.Tests/
dotnet test --nologo -v q
! grep -rn 'stats' tests/TaskApi.Tests/EndpointSecurityTests.cs
