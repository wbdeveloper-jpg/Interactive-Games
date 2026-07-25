using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;


public class Experiments : MonoBehaviour
{

    // "abcdsse"

    private void Largest()
    {
        


        
    }

    Queue<int> input = new Queue<int>();
    Queue<int> output = new Queue<int>();

    void Push(int value)
    {
        if (input.Count > 0) { 
            output.Enqueue(input.Dequeue());
        }

        input.Enqueue(value);
    }

    void Pop()
    {
        if (input.Count > 0) { input.Dequeue(); }
        else output.Dequeue();
    }

}

