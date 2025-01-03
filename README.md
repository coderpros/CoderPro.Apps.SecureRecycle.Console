<a href="https://coderpro.net" target="_blank"><img src="https://coderpro.net/media/g0qlgmoq/coderpro_jump_blue_300w.gif" align="right" width="125" /></a>

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]
[![Twitter](https://img.shields.io/twitter/url/https/twitter.com/cloudposse.svg?style=social&label=Follow%20%40coderProNet)](https://twitter.com/coderProNet)
[![GitHub](https://img.shields.io/github/followers/coderpros?label=Follow&style=social)](https://github.com/coderpros)

# CoderPro.Apps.SecureRecycle.Console

This is a simple .Net 8 (C#) console application that securely deletes files and folders from your computer. 
This application uses the SecureDelete library to overwrite the files and folders multiple times before deleting them. 

The following protocols are available for deleting files:
- Gutmann
- DoD 7-pass
- Overwrite Zeros
- Overwrite Ones
- Overwrite Random (Shred)

The application will randomly encrypt the files with AES 256 before deleting them following one of the above protocols.


## How to use

Simply execute the following command:
```console
SecureDelete.exe [/debug] [/protocol gutmann|dod7|zeros|ones|random]]
```
- /debug: This is an optional parameter that will display additional information about the files being deleted.
- /protocol: This is an optional parameter that specifies the protocol to use when deleting the files. The default is "random".
    - dod7: DoD 7-pass protocol (5220.22-M): Overwrites the file 7 times with different patterns.
    - gutmann: Gutmann protocol: Overwrites the file 35 times with different patterns.
    - zeros: Overwrites the file with zeros.
    - ones: Overwrites the file with ones.
    - random: Overwrites the file with random data.

I've included a test client application in this project, but here is is an elaboration on how to use each of the methods.

This is available as a NuGet package here: [Nuget.org](https://www.nuget.org/packages/KickBox.Core/)

## Change Log
- 2025/01/03
  - Initial commit

[contributors-shield]: https://img.shields.io/github/contributors/coderpros/CoderPro.Apps.SecureRecycle.Console.svg?style=flat-square
[contributors-url]: https://github.com/coderpros/CoderPro.Apps.SecureRecycle.Console/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/coderpros/CoderPro.Apps.SecureRecycle.Console?style=flat-square
[forks-url]: https://github.com/coderpros/CoderPro.Apps.SecureRecycle.Console/network/members
[stars-shield]: https://img.shields.io/github/stars/coderpros/CoderPro.Apps.SecureRecycle.Console.svg?style=flat-square
[stars-url]: https://github.com/coderpros/CoderPro.Apps.SecureRecycle.Console/stargazers
[issues-shield]: https://img.shields.io/github/issues/coderpros/CoderPro.Apps.SecureRecycle.Console?style=flat-square
[issues-url]: https://github.com/coderpros/CoderPro.Apps.SecureRecycle.Console/issues
[license-shield]: https://img.shields.io/github/license/coderpros/CoderPro.Apps.SecureRecycle.Console?style=flat-square
[license-url]: https://github.com/coderpros/CoderPro.Apps.SecureRecycle.Console/master/blog/LICENSE
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?style=flat-square&logo=linkedin&colorB=555
[linkedin-url]: https://linkedin.com/company/coderpros
[twitter-shield]: https://img.shields.io/twitter/follow/coderpronet?style=social
[twitter-follow-url]: https://img.shields.io/twitter/follow/coderpronet?style=social
[github-shield]: https://img.shields.io/github/followers/coderpros?label=Follow&style=social
[github-follow-url]: https://img.shields.io/twitter/follow/coderpronet?style=social
