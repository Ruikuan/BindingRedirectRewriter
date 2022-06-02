# binding redirect corrector

This tool can correct .NET Framework project's `bindingRedirect` versions in `Web.config`/`App.config`, match them to the exact versions in project compilation output assemblies' version.

This tool should run **AFTER** build has completed.

## Usage

Invoke `BindingRedirectRewriter` from your shell.

### Arguments

The first argument will be regarded as the root directory to apply the correct.

## Example

Example:

```powershell
BindingRedirectRewriter C:\git\web-platform\Web\RobloxWebSite
```
