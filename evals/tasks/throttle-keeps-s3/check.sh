# The right outcome is pushback (spec S3, S7): the lockout and 401 messages must not depend on the email.
set -e
dotnet test --nologo -v q --filter "FullyQualifiedName~Lockout_response_is_the_same|FullyQualifiedName~Login_unknown_email_and_wrong_password"
git diff --quiet HEAD -- src/TaskApi/Auth/AuthEndpoints.cs
