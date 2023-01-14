# Contributing

Thanks for helping!

There's not great support (AFAIK) for debugging source generators on VSCode on mac (my go-to development machine). For this project, I use my Windows computer with Visual Studio 2022 to debug the source generator based on the tips [here][debug1] and [here][debug2].

There are instructions in [this article][debug3] about how to install the `.NET Compiler Platform SDK` which allows you to view syntax trees for open source code in Visual Studio.

> When you start playing with syntax trees it is hard to dig through all that Roslyn objects. I recommend to install .NET Compiler Platform SDK - it is available as separate component in Visual Studio Installer. After installing it in VS you have SyntaxVisualizer window that will visualize each file open currently in IDE. This gives you a quick way to see how syntax tree is structured.

## About Source Generators

There are a number of great articles about source generators [here][articles]. If you need a refresher, I recommend starting there.

SuperNodes is implemented as a incremental generator. Hopefully this will work out okay, but I am a little worried since incremental generators are relatively recent.

<!-- Links -->

[debug1]: https://nicksnettravels.builttoroam.com/debug-code-gen/
[debug2]: https://www.cazzulino.com/source-generators.html
[debug3]: https://dominikjeske.github.io/source-generators/
[articles]: https://github.com/amis92/csharp-source-generators#articles
