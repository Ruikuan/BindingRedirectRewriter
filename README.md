# binding redirect corrector

[![Build Status][ci-badge]][ci]
[![NuGet][nuget-badge]][nuget]

[ci]: https://github.com/Ruikuan/BindingRedirectRewriter/actions/workflows/dotnet.yml?query=branch%3Amaster
[ci-badge]: https://github.com/Ruikuan/BindingRedirectRewriter/actions/workflows/dotnet.yml/badge.svg
[nuget]: https://www.nuget.org/packages/Ruikuan.BindingRedirectRewriter/
[nuget-badge]: https://img.shields.io/nuget/v/Ruikuan.BindingRedirectRewriter.svg?style=flat-square

This tool can correct .NET Framework project's `bindingRedirect` versions in `Web.config`/`App.config`, match them to the exact versions in project compilation output assemblies' version.

This tool should run **AFTER** build has completed.

## Usage

### Installation

```
dotnet tool install --global Ruikuan.BindingRedirectRewriter 
```


Invoke `BindingRedirectRewriter` from your shell.

### Arguments

The first argument will be regarded as the root directory to apply the correct.

## Example

Example:

```powershell
BindingRedirectRewriter C:\git\web-platform\Web\RobloxWebSite
```
