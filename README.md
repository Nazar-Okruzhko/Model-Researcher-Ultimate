# Model-Researcher-Ultimate

Hello, Nazar Okruzhko here maybe some of you know me from Reddit or Sketchfab, I am an ID Tech 5 expert, I am very newbie/noob at reverse engineering just started a few months ago but made some progress over the past few weeks, expecially for ID Tech 5 and 6 games...

 

I've made a tool called "Model Researcher Ultimate" for Extracting 3D Models from any games, it's based on the original "Model Researcher" which is based on the "Model Inspector".

<img width="1920" height="1080" alt="Screenshot (1500)" src="https://github.com/user-attachments/assets/c7952279-fea6-4f96-851b-3e0e1568e839" />

[BETA Screenshot] - It's a fully Open Source Program written in C#

After getting a fully ready Un/Decompressed and Un/Decrypted Binary 3D Model file, this program can be used to find how the 3D Data is stored, Vertecies, UV Coords and Faces, in any unsupported format by finding the Offsets (Entry points) for Vertecies, UV Coords and Faces and finding how the Data is organized (Separate Buffers or Structured Buffers with Vertecies and UV Coords together separated by padding), we can find a Vertex and Face counts as well as the Buffer header/markers and then just export models into OBJ Files. I made this tool on December 28th, 2025 as for the Ultimate edition.
Release build requires .Net 6.0 to run.

I've managed to dig up a Legendary BJ Blazkowicz model using Model Researcher Ultimate and really liked the idea of the program being for the first time able to extract 3D model from any proprietary 3D Model files. I liked the design so much that really wanted to try something like that creating an updated, Model Researcher "Ultimate" . The version 1.0.0 is currently not fully stable, ignore the Normals extraction, I'll probably replace with the final Release and will start updating the program.

Main files:
ModelResearcherUltimate.cs
ModelResearcherUltimate.csproj



Main features:

All in One Windows Bundle <- Super comfortable fast-paced menu
Made to eat the multitasking
Improved Camera navigation
Comfortable HEX Viewer
Drag and drop support
Fast Copy-Pasting OBJ file
C# .NET for good performance
I don't really recommend hitting the Print button cause the colored byte updating is still a super slow process
Normals are Auto-Generated in this program so forget about it for a second and Scripting tab is only for feature capabilities

Since It's a fully Open Source program I proudly allow for everyone to modify and redistibute the program how they want.

(I am planning to make tools for other ID Tech Engines, and Mario games too).
