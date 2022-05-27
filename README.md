# VfxTool
A tool for compiling and decompiling Fox Engine vfx files, which store compiled VFX node graphs.

## Usage
Drag and drop one or more .vfx files onto the .exe to unpack them to .xml files. Do the same with .xml files to repack them into .vfx format.

## On unsupported node types
The Definitions folder contains JSON metadata defining each VFX node type. These tell VfxTool how to unpack and repack the node types. When VfxTool encounters an unfamiliar node, it will tell you the offset in the file to help you find and reverse it. Just create a new JSON file in the format of the existing ones and the tool should be able to parse it next time you run the tool.

Fortunately, the format of VFX nodes is very regular and documented here: https://metalgearmodding.fandom.com/wiki/VFX#Nodes

If you have any corrections or new node definitions, please feel free to submit them so other users can make use of them. Currently, there are definitions for every VFX node in TPP and several in GZ, although quite a few in GZ remain unsupported.

## Credits
* Joey for help with reversing the .VFX format.
* OldBoss for transcribing the wiki's VFX node data into JSON files
* caplag for reversing the following nodes: FxMultipleVectorNode, FxUpdateSleepControlNode, FxPlaneShapeNode, FxConstantMaterialNode, among many others, especially in GZ
* BobDoleOwndU for implementing read/write support for unknown1, also improving output path handling
