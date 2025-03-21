# GPU Skinned Fur Instance Rendering

This Unity demo showcases an implementation of shell-based fur rendering using GPU instancing on skinned meshes. The project demonstrates how to render multiple fur shell layers in a single pass, while maintaining support for skeletal animation.

## Features

- GPU Instancing support for skinned meshes using `SkinnedMeshRenderer.GetVertexBuffer`
- Shell-based fur rendering in a single draw pass  
- Fully animated characters with bone skinning  
- Efficient rendering using Unity's `CommandBuffer.DrawProcedural` and custom shader logic

## Requirements

- Unity 2021.2.7f1  

## Demonstration

https://github.com/user-attachments/assets/45dbbd15-5790-4718-b412-4fb882627726

## Technical Details

A detailed explanation of the technique and implementation is available on Medium:  
👉 [GPU Skinned Fur Instance in Unity](https://medium.com/@TienYoung/gpu-skinned-fur-instance-in-unity-ccc0668b4202)

