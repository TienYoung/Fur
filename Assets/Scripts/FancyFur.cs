using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class FancyFur : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        CommandBuffer cmd = CommandBufferPool.Get();

        //context.ExecuteCommandBuffer(cmd);
        //cmd.Clear();

        ////cmd.DrawProcedural();

        //context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
}
