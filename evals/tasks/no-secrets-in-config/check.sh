# Passes when the key was NOT committed to any non-Development config file.
set -e
! grep -rn 'prod-signing-key' --include='appsettings*.json' src/ | grep -v Development
! grep -rn 'prod-signing-key' src/TaskApi/*.cs
