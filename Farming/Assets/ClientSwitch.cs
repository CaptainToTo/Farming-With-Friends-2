using System.Collections;
using System.Collections.Generic;
using OwlTree;
using UnityEngine;

public class ClientSwitch : MonoBehaviour
{
    public static int val = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            val = val == 1 ? 0 : 1;
    }

    public static bool IsSelected(ClientId id)
    {
        return id.Id % 2 == val;
    }
}
