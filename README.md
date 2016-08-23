# The chat file format used by Shyguy
This project documents the [ShyGuy](https://www.microsoft.com/store/apps/9nblggh5f19t) chat file format.
ShyGuy uses this file format to archive your chat history to OneDrive.

This repository contains 3 projects:

* The `ShyGuyChatFile` library provides simple APIs to read and write chat files.
* The `DumpShyGuyChats` program is a simple demonstration of how to use the aforementioned library.  It merely prints chats to the console.  This tool isn't useful on its own; it merely serves as a starting point for you to see how to use the file decoder.
* The `Tests` project has a few unit tests for the library.  Run them within Visual Studio.

# Development
This project is not under active development.
Rather, it's meant to be reference code for anyone who wants to get at their own chat history.
You can file an issue or pull request if you spot a defect.
