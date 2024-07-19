using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnLightScript : MonoBehaviour
{
    public Light[] lights;
    public Light lightButton;
    public Light lightMain;

    public void SwitchLight() {

        if (lights[0].enabled == false)
        {
            foreach (Light i in lights)
            {
                i.enabled = true;
            }
           // lightMain.enabled = false;
            lightButton.color = Color.red;
        }
        else {
            foreach (Light i in lights)
            {
                i.enabled = false;
            }
           // lightMain.enabled = true;
            lightButton.color = Color.green;
        }
    } 
}
