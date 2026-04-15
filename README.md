# Model-Researcher-Ultimate
Hello, it's Nazar Okruzhko, some of you might know me from Reddit or Sketchfab, I am an ID Tech 5 expert in terms of Reverse Engineering, I was used to be a real Noob at reverse engineering, just started a some time ago but Reversing ID Tech 5 & ID Tech 6 & ID Tech 7 games made me some unreal progress over the past just a few weeks...

I've made a tool called "Model Researcher Ultimate" for Extracting 3D Models from any games, it's design is based on the original "Model Researcher" which design is based on the "Model Inspector" which is based of "Hex2Obj".

<img width="1920" height="1080" alt="Screenshot (3232)" src="https://github.com/user-attachments/assets/d6226503-6976-43c6-a967-897d1f88cff9" />

![Static Badge](https://img.shields.io/badge/Minimal%20Requirements%3A-Windows%2010-default)

"What is Model Researcher Ultimate?"

It's essentially a Binary Reader BE/LE that reads values in Formats like "Float/Half-Float/Short-Signed" depending on the parameter values from the Config Tab, as shown in the HEX Viewer marking the Values it reads in different colors, it then writes them out sequentially 3 or 2 Values for each line and adds corresponding prefix for them (v = for vertecies, vn = for normals, vt = for uv coords, f = for faces) & writes it into the OBJ File Window, it then re-reads this content as OBJ File and Renders it out in 3D Viewer & UV Map Box... The final assembled 3D Model, then exposes the 3D File's internal structure in HEX Viewer & Configs from "Mesh" Tab or "Skeleton" Tab for Rig, allowing then to be used in "Mesh_Extraction" tab allowing for full 3D Model extraction with all the sub-meshes without writing a single line of Python Code...

(I've managed to pull off a Legendary BJ Blazkowicz model using Original Model Researcher! At this point I knew that nothing is impossible, and I've became the Developer of the Ultimate Reboot, So I really liked the idea of this program being for the first time able to extract 3D model from any Proprietary 3D Model files by any begginer without any deep knowledge. I liked the design so much, that I've literally created my own Model Researcher "Ultimate", I really liked this software, I've also started Programming on GitHub after reaching posting over +530 Different Models on SketchFab, I've made my ultimate response).\
The version 1.0 is currently stable!

Main new features:\
• All in One Windows Bundle\
• Made to eat the multitasking\
• Improved Camera navigation\
• Comfortable HEX Viewer\
• Invalid Value highlightion is back!\
• The "Auto-Update" feature is back!\
• Switch between HEX <-> DEC Base Address mode\
• Wireframe Render mode Option
• Fast Copy-Pasting OBJ file\
• Drag and drop support\
• Search for & Vertex Buffer search for instant Vertex Buffer exposure\
• Got to Buffer Offset/Buffer End, Set as Offset/Count in context menu\
• Potential Count Value highlightion in HEX Viewer\
• The "Detect-Size" checkbox for all Buffers\
• C# .NET for good performance\
• F1 F2 F3 F4 - Shortcut hacks (Old)\
• The "Mesh_Extraction" Tab for Scriptless Mesh extraction\
• The "Skeleton" Tab For Rigged Model Extraction\
• PMR Parameter files (Load/Save & Drag and Drop)\
• Diffuse textures (Multi-submesh support)

(I am also planning to make tools for other ID Tech Engines, and Mario games too).

## Getting Started



https://github.com/user-attachments/assets/e5cbb8a6-0b10-475e-8404-48a3dce611b8

Model Researcher Ultimate is a program for Reverse Engineering/Studying binary 3D model files.

This tool allows for exposing Binary 3D Data files for:\
• Vertex: Offset, Count, Padding, Format\
• UVs: Offset, Count, Padding, Format\
• Face: Offset, Count, Padding, Format\
• Normals: Offset, Count, Padding, Format\
(Which are the actual key elements for extracting full 3D Data from binary files)

Example "PMR" Parameter file:

    [Big-Endian]
    
    ROOT:
    VB 0x7BFD0, 4672, 20, XZY, Float
    VTB 0x7BFDC, 4672, 24, UV, Float
    FB 0xA07D0, 26340, 0, Triangles, Short
    
    Mesh 1:
    VB 0x5B55, 7854, 20, XZY, Float
    VTB 0x5B61, 7854, 24, UV, Float
    FB 0x43115, 41588, 0, Triangles, Short
    
    Mesh 2:
    VB 0x167, 418, 20, XZY, Float
    VTB 0x173, 418, 24, UV, Float
    FB 0x35A7, 2306, 0, Triangles, Short

[Shortcuts: F1 Print, F2 Render, F3 View UVs, F4 Flip UVs]\
[Tool has support for Multi-Submesh system, Diffuse Textures, Parameter files and Scripting]\
[The example script for BMD6MODEL files is present as default in-program script]

## INTRODUCTION
But how do all those models store their 3D Data?\
Well, the answer is simple, there is absolutelly no magic here, All 3D Models are just made up of *Vertecies*, *Normals*, *UV Coordinates* and *Faces*, (Additionally with Blend Indices, Blend Weights & Skeleton Data but it's only and only for rigged models, anything else is not an actual 3D Model Data), all of them can be directly converted into the OBJ File Format!\
All those 4 Elements *MUST* be stored somewhere in your file, (this place is called buffer) of course the Size "Count" and the location "Offset" will be different for each Model and finding those *Buffers Parameters will be the key in opening the gates for the full 3D Model Extraction. (The Tutorial is also available in "Help" -> "Qucik Start Guide").

example.obj:

    This is how the Vertecies look like:
    v  1.0 4.0 3.0 <= X, Y, Z matrix coordinates (usually from 0.001 to 1000)
    v  2.0 3.0 4.0 <= Point values so are usually Floats/Half-Floats
    v  6.0 2.0 3.0 <= Usually stable, values don't varry to much in max and min values

    This is how Vertex normals look like:
    [not so important actually, you can even skip]
    vn  0.745 0.845 0.360 <= X, Y, Z matriz coordinates (usually from -1 to 1)
    vn  0.320 0.625 0.270 <= Point values so are usually Floats/Half-Floats, so "v2 x, y, z"
    vn  0.430 0.320 0.390 <= Usually stable, values don't warry much in max and min values

    This is how UV Vertex coords look lke:
    vt  0.2 0.3 <= 2D coordinate of the first vertex (usually from 0.0001 to 1.0, Tiled UV Maps go beyond 1.0)
    vt  0.5 0.2 <= Point values so are usually Floats/Half-Floats
    vt  0.3 0.1 <= Usually stable, values don't warry to much in max and min values

    This is how faces looks like:
    f  1 2 3 <= Takes all those previous vertecies and makes a "Face", a triangle out of them


The result is a simple triangle that has it's own UV Map too and Shading.\
• The Vertecies Build up individual unique placed Flaoting points\
• The Faces connect them together\
• The UV Coordinates Map texture to a those points\
• The Normals tell how Shading wil work for those points\
• (The Normals are optional and can be auto generated...)\
This is how the simplest 3D Model, it's in OBJ File Format and that's how all Binary Models store their 3D Data, the only problem is that they are in Binary 💀.

### Buffers and Binary Data

Binary 3D Model formats have only two ways of storing their 3D Data (Aside faces) in a Separate way and Structured way, here is how it looks like:

Sequentional mode:

vertex_buffer = [

    v1  1.0 4.0 3.0 <= X, Y, Z matrix coordinates (usually from 0.01 to 1000)
    v2  2.0 3.0 4.0 <= Point values so are usually floats, so "v2  x, y, x"
    v3  6.0 2.0 3.0 <= Usually stable, values don't varry to much in max and min values
    ...
]

face_buffer = [

    f1  1 2 3 <= Takes all those previous vertecies and makes triangle out of them, so "f1  v1, v2, v3"
    ...
]

uv_coords_buffer = [

    vt1  0.2 0.3 <= 2D coordinate of the first vertex (usually from 0.1 to 1.0)
    vt2  0.5 0.2 <= Point values so are usually floats, so "vt2  u, v"
    vt3  0.3 0.1 <= Usually stable, values don't warry to nuch in max and min values
    ...
]

vertex_normals_buffer = {

    vn1  0.745 0.845 0.360 <= X, Y, X matrix coordinates (usually from 0.01 to 1)
    vn2  0.320 0.625 0.270 <= Point values so are usually floats, so "v2, x, y, z"
    vn3  0.450 0.310 0.390 <= Usually stable, values don't warry much in max and min values
    ...
}

Blocked mode:

buffer = [

    {v1  1.0 4.0 3.0, vt1  0.2 0.3, vn1  0.745 0.845 0.360}
    {v2  2.0 3.0 4.0, vt2  0.5 0.2, vn2  0.320 0.625 0.270}
    {v3  6.0 2.0 3.0, vt3  0.3 0.1, vn3  0.450 0.310 0.390}
    ...
]

### BINARY DATA

The data in each file can be viewed as binary no matter if it was readable or unreadable or even empty before in notepad, viewing it in binary will spoil immediatelly everything.
And while binary files are all the same, the way we read it changes drastically everything! To view your binary file yiou must dump HEX from it or load it into HEX Viewer:

Example file:

    Addres:   HEX Bytes:                                        ASCII:
    0012BFC0  48 53 68 61 70 65 5F 31 37 00 00 00 00 00 01 00   HShape_17....... <= First line contains ASCII strings
    0012BFD0  00 00 0A 00 00 00 22 00 00 10 00 00 00 00 0C 00   ......"......... <= Second line does not contain ASCII strings
    0012BFE0  00 00 61 32 76 2E 6F 62 6A 43 6F 6F 72 64 01 00   ..a2v.objCoord.. <= Third line contains ASCII strings
    0012BFF0  00 00 FF FF FF FF 02 00 00 00 47 04 00 00 82 56   ..........G....V <= Fourth line contains interesting "00 00 FF FF FF FF" buffer mark
    0012C000  F9 40 39 94 59 43 76 26 13 41 BB 61 FB 40 5A A4   .@9.YCv&.A.a.@Z. <= Fifth line starts containg the actual float Vertex coordinates! But looks random in ASCII strings!
    0012C010  5B 43 95 B7 00 41 8F 70 CB 40 C1 4A 5B 43 31 08   [C...A.p.@.J[C1. <= Sixth line contains actual float Vertex coordinates! But looks random in ASCII strings!
    0012C020  12 41 8A 8E C9 40 E7 5B 59 43 E8 82 1D 41 90 A0   .A...@.[YC...A.. <= Seventh line contains actual flaot Vertex coordinates! But looks still random in ASCII strings!
    0012C030  62 40 21 90 58 43 05 DD 1C 41 BC B3 78 40 D7 63   b@!.XC...A..x@.c <= Eight line contains actual float Vertex coordinates! But looks again random in ASCII strings!


But what are those floats, half-floats, shorts and ASCII?
The Bits are the smallest units of computer data they are either 0 or 1 and comma.
The Bytes hgovewer is a combined 8 Bits that can actually start representing some data. Those are Bits ranging from 0 to 255, where 0 is also precieved as an important value (So 256 combinations), (I represented them in HEX, 0-F values, so a 256 combinations)
Here is one Byte for example: 10110111 (32 16 8 4 2 1 = 256 bits as sum), combining Bytes together we can make multiple data types.

    This are all of the data types:
    Byte/Char => 1 Byte, unsigned/signed (8 Bits)                                      |Example: 48 <= H | ASCII
    Word/Short => 2 bytes, unsigned/signed (16 Bits)                                   |Example: 48 53 <= HS | ASCII
    Dword/Int => 4 bytes, unsigned/signed (32 Bits)                                    |Example: 48 53 68 61 <= HShap | ASCII
    ULONG32/Long => 4 Byte, unsigned/signed (32 Bits)                                  |Example: 48 53 68 61 <= HShap | ASCII
    ULONG64/Long Long => 8 Byte, unsigned/signed (64 Bits)                             |Example: 48 53 68 61 70 65 5F 31 <= HShape_17 | ASCII
    float => 4 bytes, for represnting floating point values (32 Bits)                  |Example: 48 53 68 61 <= HShap | ASCII
    double => 8 bytes, for representing more precise floating point values (64 Bits)   |Example: 48 53 68 61 70 65 5F 31 <= HShape_17 | ASCII
    String/Char => A Sequence/Array of Characters terminated by the nulll character    |Example: 48 53 68 61 70 65 5F 31 <= HShape_17 | ASCII

### Big-Endin vs Little-Endian:

Reading in Big-Endian for example a float byte will read it normally, left-to-right 48 53 68 61 "HShap", where's Little-endin reads byte in reverse order, right-to-left 61 68 53 48 "paSH".
Big-Endians were mainly used in PS3, Xbox360 and Wii platforms where Little-Endians are mainly in Windows, PS4, Xbox One, Nintendo Switch.

### TRYING TO REVERSE THE BINARY 3D FORMAT

But how do we actually apply this info into reverse engineering the binary 3D file format structure and even converting it into an OBJ Model.
Assuming that you have the actual decompressed/uncompressed and decrypted/unencrypted binary 3D model file, you can actually visualize the 3D Data geometry while analyzing the HEX from it in realtime!
ModelResearcherUltimate is the program that will enable this opportunities.

First of, Level 1: Start with vertecies count 500, type: float, carefully try different offsets while printing the values and render it too, until you see a countinous very stable output without insanelly big or small values. (from 0.001 to 1000).
If nothing works try with different Endianess, then try a different type (unlikely). If the mesh appears but random vertecies appear too that means that the Data structure is sctructured and you need to try different Padding or even Pad inters sometimes.

Second of, Level 2: Start with vertex UV coordinates count [exactly how many vertecies], type: float, carefully try different offsets while printing the values and rendering it too, until you see a countinous stable output without insanelyy big or small values (from 0.0001 to 1.)
If nothing works try different type, since you already know the Endianes and Structure.

Third of, Level 3: Start with faces, they are actually very carefully linked with vertecies, so the errors will constantly appear, carefully try different offsets while printing the values, don't render it, it will often just throw the errors.
You will need see the full values without floating points that are very stable in output without big and small values, if nothing works try different type or even the format.
Fourth of, Level 4: [To be honest I didn't know what to write here, normals are pretty useless though, you can just flip them and calculate, very easily in programs like Blender in just a few clicks, so it's not worth your brainstorming!]

### Practical steps:

Here is how BAD Data will look like:

good.obj:

    v  -0.0000 -0.0000 -184538016.0000
    v  -0.0000 15.7924 -158665664.0000
    v  -0.0000 90990377942005974930976407552.0000 -17551224.0000
    v  -0.0000 -3386287.2500 -115467744.0000
    v  -0.0000 15397417210601645679040601784320.0000 -22963316.0000
    v  -0.0000 15397417210601645679040601784320.0000 -22963316.0000
    ...
    
    vt  0.0000 1785889664.0000
    vt  0.0000 140283808776479363868647227392.0000
    vt  0.0000 10997215558668704718782464.0000
    vt  0.0000 -516472.2188
    vt  0.0000 -0.0000
    vt  0.0000 0.0000
    ...
    
    f  57856 10240 3073
    f  3073 64769 57856
    f  31744 64768 3072
    f  57857 64768 58112
    f  57856 58112 58368
    f  58112 59136 58368
    ...
    
    [random, disoriented pattern, extreamly low and extreamly big values occur]

Here is how GOOD data looks like:

bad.obj:

    v  -0.0733 0.0012 1.6030
    v  -0.0735 -0.0118 1.6023
    v  -0.0776 -0.0146 1.5900
    v  -0.0718 -0.0247 1.6005
    v  -0.0784 0.0009 1.5913
    v  -0.0784 0.0009 1.5913
    ...
    
    vt  0.0008 0.6221
    vt  0.0316 0.6229
    vt  0.0344 0.6543
    vt  0.0628 0.6246
    vt  0.0008 0.6539
    vt  0.9978 0.6533
    ...
    
    f  226 296 268
    f  268 253 226
    f  124 253 268
    f  226 253 227
    f  226 227 228
    f  227 231 228
    ...
    
    [strong countinous repating pattern, values are pretty much very similiar]

### Finally

Changing Offset (oftenly) and Endianess or Type have the stongest influence and will instanly give the different results including bad data drastically turning into a GOOD data so keep that  in mind and play with those offsets.

There is just one small but very important step left, most of the time those binary files leave also values like Vertex count (UV Coords and Vertex Normals count is the same as Vertex always), Face count, buffer mark and even Vertex stride! (Vertex Stride = Vertex Padding + 12, UV Coords stride = UV Coords stride + 8). They are essentially at the begginning of the mesh buffer  and are pretty easy to find and are always placed in the same way hovewer, this time I personally recommend finding them using the dedicated HEX viewer, my recommendadions are IM Hex, truly the open-sourse king in terms of ease of use.

### **Skeleton support is now AVAILABLE!**
<img width="1920" height="1080" alt="Screenshot (2316)" src="https://github.com/user-attachments/assets/8ad4ccc1-484e-4d7f-bdc9-3043ec2c1862" />

### DDS, TGA and JPG Texture support added to version v1.0!!!
