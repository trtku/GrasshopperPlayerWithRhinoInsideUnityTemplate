using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Rhino;
using RhinoInside.Unity;
using System;
using System.Linq;

public class GrasshopperInUnity : MonoBehaviour
{
    public static GrasshopperInUnity instance;

    public GameObject uiParent;
    public GameObject sliderPanelPrefab;
    public GameObject geoPrefab;
    public GameObject physicalSliderPrefab;

    #region mono behaviour

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!Startup.isLoaded)
        {
            Startup.Init();
        }

        Rhino.Runtime.HostUtils.RegisterNamedCallback("FromGHMesh", FromGHMesh);
        Rhino.Runtime.HostUtils.RegisterNamedCallback("FromGHClearMesh", FromGHClearMesh);
    }

    // Update is called once per frame
    void Update()
    {
        SendCoord("Slider_Button", "point");
    }

    #endregion

    #region to GH functions

    public void SendCoord(string name, string key)
    {
        GameObject ps = GameObject.Find(name);
        var pt = ps.transform.position.ToRhino();

        using (var args = new Rhino.Runtime.NamedParametersEventArgs())
        {
            args.Set(key, new Rhino.Geometry.Point(pt));
            Rhino.Runtime.HostUtils.ExecuteNamedCallback("ToGrasshopper", args);
        }
    }
    #endregion

    #region from GH functions

    void DeleteMeshes(string id)
    {
        var objs = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == id);
        foreach(var parentGB in objs) {
            var meshFilters = GetComponentsInChildren<MeshFilter>();

            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    DestroyImmediate(meshFilter.sharedMesh);
                }
            }

            Destroy(parentGB);
        }
    }
    void FromGHMesh(object sender, Rhino.Runtime.NamedParametersEventArgs args)
    {
        if (Application.isPlaying)
        {
            Rhino.Geometry.GeometryBase[] values;
            System.Drawing.Color diffuse;
            string id = "";

            if (args.TryGetString("id", out id))
            {
                args.TryGetColor("diffuse", out diffuse);
                args.TryGetGeometry("mesh", out values);

                if (values.Length > 0)
                {
                    var parentGB = new GameObject(id);
                    for (int i = 0; i < values.Length; i++)
                    {
                        GameObject instance = (GameObject)Instantiate(geoPrefab);
                        instance.transform.SetParent(parentGB.transform);

                        var meshFilter = instance.GetComponent<MeshFilter>();
                        meshFilter.mesh = (values[i] as Rhino.Geometry.Mesh).ToHost();
                        var meshRenderer = meshFilter.GetComponent<MeshRenderer>();

                        var mat = meshRenderer.material;

                        mat.color = new Color32(diffuse.R, diffuse.G, diffuse.B, 255);
                    }
                }
            }
        }
    }
    void FromGHClearMesh(object sender, Rhino.Runtime.NamedParametersEventArgs args)
    {
        string id = "";
        if (args.TryGetString("id", out id))
        {
            DeleteMeshes(id);
        }
    }

    #endregion

    #region rhino / gh window

    public void OpenGH()
    {
        string script = "!_-Grasshopper _W _S ENTER";
        Rhino.RhinoApp.RunScript(script, false);
        
    }

    public void OpenRhino()
    {
        ShowWindow(RhinoApp.MainWindowHandle(), 1);
        BringWindowToTop(RhinoApp.MainWindowHandle());
    }
    

    [DllImport("USER32", SetLastError = true)]
    static extern IntPtr BringWindowToTop(IntPtr hWnd);

    [DllImport("USER32", SetLastError = true)]
    static extern int ShowWindow(IntPtr hWnd, int nCmdShow);

    #endregion
}

