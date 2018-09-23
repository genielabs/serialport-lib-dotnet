# Contributing

## How to contribute to this repository

#### **Did you find a bug?**

* **Ensure the bug was not already reported** by searching on GitHub under [Issues](https://github.com/genielabs/serialport-lib-dotnet/issues).

* If you're unable to find an open issue addressing the problem, [open a new one](https://github.com/genielabs/serialport-lib-dotnet/issues/new).
Be sure to include a **title and clear description**, as much relevant information as possible, and a **code sample** or an **executable test case** demonstrating the expected behavior that is not occurring.

#### **Did you write a patch that fixes a bug?**

* Open a new GitHub pull request with the patch.

* Ensure the PR description clearly describes the problem and solution.
Include the relevant issue number if applicable.

#### **Did you fix whitespace, format code, or make a purely cosmetic patch?**

Changes that are cosmetic in nature and do not add anything substantial to the stability, functionality,
or testability of this project, will generally not be accepted unless discussed via the [issue tracker](https://github.com/genielabs/serialport-lib-dotnet/issues).

#### **Do you intend to add a new feature or change an existing one?**

File a new *[enhancement issue](https://github.com/genielabs/serialport-lib-dotnet/issues/new?labels=enhancement)*.

#### **Do you have questions about the source code?**

File a new *[question issue](https://github.com/genielabs/serialport-lib-dotnet/issues/new?labels=question)*.

#### **Coding styles and conventions**

This project follows *Microsoft .Net* [coding conventions](https://docs.microsoft.com/dotnet/csharp/programming-guide/inside-a-program/coding-conventions) and [naming guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions).

##### Releasing a new version

To release a new version push a new tag using the format:

`<major>.<minor>.<build>`

examples: `1.0.16`, `1.0.20-pre1`

When a new tag is submitted the CI system will build the project, run tests and package assets (.dll and .nupkg distribution files). The NuGet package will be automatically published and assets will be also uploaded to the new release tag on GitHub repository.

#### Join SerialPortLib team!

SerialPortLib is a volunteer effort. We encourage you to pitch in and join the team!

Thanks! :heart:

