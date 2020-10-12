# VfxTool
A tool for compiling and decompiling Fox Engine vfx files, which store compiled VFX node graphs.

## Usage
Drag and drop one or more .vfx files onto the .exe to unpack them to .xml files. Do the same with .xml files to repack them into .vfx format.

## On unsupported node types
It's not only likely but expected that the tool will fail to unpack many files due to missing node definitions. Unfortunately, there are close to 100 different VFX node types in the engine, and the format differs for each node tpe, meaning that VfxTool requires metadata about a node in order to parse it.

The Definitions folder contains JSON metadata defining each node type. These tell VfxTool how to unpack and repack the node types. When VfxTool encounters an unfamiliar node, it will tell you the offset in the file to help you find and reverse it. Just create a new JSON file in the format of the existing ones and the tool should be able to parse it next time you run the tool.

Fortunately, the format of VFX nodes is very regular and documented here: https://metalgearmodding.fandom.com/wiki/VFX#Nodes

If you have any corrections or new node definitions, please feel free to submit them so other users can make use of them. Currently, there are definitions for almost half of the known node types.
