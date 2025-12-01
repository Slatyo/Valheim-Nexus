using System.Security;
using System.Security.Permissions;

// This allows access to internal/private members of game assemblies
// Required for accessing publicized assemblies
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
[module: UnverifiableCode]
