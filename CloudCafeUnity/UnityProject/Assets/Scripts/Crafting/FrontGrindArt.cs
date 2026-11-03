using UnityEngine;
namespace CloudCafe.Crafting {
 // Temporary mesh poses: the camera, not a postprocessed screenshot, renders the pixel grid.
 public sealed partial class FrontGrindArt {
  public readonly Transform Tool,Hand,Grounds,Switch,Shadow,Distributor,Tamper,PrepHand;
  public readonly Transform BrewButton,Cup,CoffeeSurface,DockHint,PrepHint,CupHint;
  public readonly Vector3 GroupSeat=new Vector3(2.5f,.82f,.05f),CupHome=new Vector3(3.7f,.065f,-.65f),CupSeat=new Vector3(2.5f,.19f,.02f);
  public readonly Vector3 DistributorHome=new Vector3(1.3f,.07f,-.15f),TamperHome=new Vector3(1.4f,.07f,-.72f);
  public readonly Camera Camera;
  public readonly RenderTexture Pixels;
  public readonly Vector3 Mat=new Vector3(.64f,.12f,-.65f),Dock=new Vector3(-.64f,.66f,.14f);
  public readonly Vector3 Outlet=new Vector3(-.64f,.99f,.14f);
  readonly Transform root; readonly Material steel,dark,light,gold,skin,coffee;
  readonly Transform[] grains=new Transform[90];
  readonly Transform[] espresso=new Transform[90];
  readonly Transform[] coffeeStreams=new Transform[2];
  Material MatColor(string name,string hex){Color c;ColorUtility.TryParseHtmlString(hex,out c);var m=new Material(Shader.Find("Legacy Shaders/Diffuse"));m.name=name;m.color=c;return m;}
  public FrontGrindArt(){
   root=new GameObject("Front workbench mesh prototype").transform;
   steel=MatColor("blue steel","#637F9E");dark=MatColor("ink blue","#202A3D");light=MatColor("warm rim","#E5D3AC");gold=MatColor("brass","#BA8A50");skin=MatColor("hand","#BB927F");coffee=MatColor("grounds","#623F2C");
   var wood=MatColor("counter wood","#7B5141");var wall=MatColor("wall","#394760");
   Cube("counter",root,new Vector3(1.8f,-.16f,0),new Vector3(10,.3f,3.3f),wood);
   for(int i=0;i<21;i++)Cube("wood seam",root,new Vector3(-3.2f+i*.5f,-.006f,0),new Vector3(.008f,.007f,3.3f),dark);
   Cube("back wall",root,new Vector3(1.8f,1.4f,1.45f),new Vector3(10,3,.12f),wall);
   Cube("brass wall trim",root,new Vector3(1.8f,.46f,1.37f),new Vector3(10,.025f,.03f),gold);
   Cube("work mat",root,new Vector3(.64f,.015f,-.65f),new Vector3(.85f,.04f,.8f),dark);
   for(int i=0;i<12;i++)Cube("mat ridge",root,new Vector3(.26f+i*.068f,.038f,-.65f),new Vector3(.018f,.006f,.74f),steel);
   var machine=new GameObject("grinder").transform;machine.SetParent(root);machine.position=new Vector3(-.64f,0,.55f);
   Cube("foot",machine,new Vector3(0,.08f,-.15f),new Vector3(.85f,.16f,.85f),dark);
   Cube("base plate",machine,new Vector3(0,.17f,-.15f),new Vector3(.8f,.035f,.8f),steel);
   Cube("motor housing",machine,new Vector3(0,.89f,.06f),new Vector3(.57f,1.4f,.47f),steel);
   Cube("front inset",machine,new Vector3(0,1.02f,-.186f),new Vector3(.44f,.82f,.02f),dark);
   Cube("front metal panel",machine,new Vector3(0,1.08f,-.205f),new Vector3(.38f,.62f,.025f),steel);
   Cube("brass badge",machine,new Vector3(0,1.3f,-.23f),new Vector3(.24f,.065f,.025f),gold);
   for(int i=0;i<6;i++)Cube("vent",machine,new Vector3(.295f,.95f+i*.05f,.04f),new Vector3(.012f,.012f,.24f),dark);
   Lathe("hopper",machine,new Vector3(0,1.59f,0),new Vector2[]{new Vector2(.13f,0),new Vector2(.28f,.06f),new Vector2(.29f,.42f),new Vector2(.27f,.44f)},steel,16);
   Cylinder("beans surface",machine,new Vector3(0,2.015f,0),.265f,.02f,coffee);
   var rng=new System.Random(19);for(int i=0;i<50;i++){float a=(float)rng.NextDouble()*6.28f,r=Mathf.Sqrt((float)rng.NextDouble())*.235f;var b=Cube("bean",machine,new Vector3(Mathf.Cos(a)*r,2.035f,Mathf.Sin(a)*r),new Vector3(.035f,.018f,.055f),coffee);b.localEulerAngles=new Vector3(0,a*57,0);}
   Ring("hopper lip",machine,new Vector3(0,2.04f,0),.3f,.277f,.018f,light);
   Cube("chute upper",root,new Vector3(-.64f,1.1f,.22f),new Vector3(.2f,.14f,.4f),dark);
   Cube("chute highlight",root,new Vector3(-.64f,1.17f,.22f),new Vector3(.2f,.025f,.4f),light);
   Cube("chute mouth",root,Outlet+new Vector3(0,.045f,0),new Vector3(.14f,.09f,.13f),steel);
   Cube("black outlet",root,Outlet,new Vector3(.105f,.008f,.095f),dark);
   // Forks support the bowl underneath; their front ends occlude it naturally in 3D.
   foreach(float side in new[]{-.15f,.15f}){Cube("cradle arm",root,new Vector3(-.64f+side,.648f,.29f),new Vector3(.035f,.035f,.49f),gold);Cube("cradle pad",root,new Vector3(-.64f+side,.67f,.13f),new Vector3(.045f,.018f,.12f),dark);}
   Switch=Cube("physical grinder button",root,new Vector3(-.64f,.92f,.297f),new Vector3(.15f,.12f,.045f),gold);Switch.gameObject.AddComponent<BoxCollider>();
   Tool=new GameObject("portafilter").transform;Tool.SetParent(root);Tool.position=Mat;
   // Continuous outer wall, rim, inner wall and floor; no filled metal disk over the opening.
   Lathe("bowl",Tool,Vector3.zero,new Vector2[]{new Vector2(.125f,0),new Vector2(.2f,.035f),new Vector2(.218f,.15f),new Vector2(.198f,.15f),new Vector2(.168f,.025f),new Vector2(0,.025f)},steel,32);
   Ring("bright bowl rim",Tool,new Vector3(0,.153f,0),.22f,.197f,.012f,light);
   Cylinder("basket bottom",Tool,new Vector3(0,.028f,0),.165f,.008f,dark);
   Cube("double spout bridge",Tool,new Vector3(0,-.027f,0),new Vector3(.24f,.05f,.055f),steel);
   foreach(float side in new[]{-.11f,.11f})Cylinder("espresso spout",Tool,new Vector3(side,-.055f,0),.022f,.045f,light);
   Cube("handle neck",Tool,new Vector3(0,.07f,-.24f),new Vector3(.105f,.08f,.19f),light);
   var handle=Cube("grab handle",Tool,new Vector3(0,.065f,-.48f),new Vector3(.13f,.11f,.38f),dark);handle.gameObject.AddComponent<BoxCollider>().size=new Vector3(1.85f,1.8f,1.3f);
   Cube("handle glint",Tool,new Vector3(-.042f,.124f,-.46f),new Vector3(.018f,.008f,.29f),steel);
   for(int i=0;i<6;i++)Cube("grip groove",Tool,new Vector3(0,.123f,-.35f-i*.047f),new Vector3(.12f,.006f,.006f),steel);
   Grounds=Lathe("separate grounds mound",Tool,new Vector3(0,.03f,0),new Vector2[]{new Vector2(0,0),new Vector2(.166f,0),new Vector2(.188f,.07f),new Vector2(.14f,.11f),new Vector2(0,.125f)},coffee,24);
   var fleck=MatColor("coffee flecks","#956546");for(int i=0;i<40;i++){float a=i*2.39996f,r=Mathf.Sqrt(i/40f)*.168f;Cube("surface fleck",Grounds,new Vector3(Mathf.Cos(a)*r,.125f-.045f*r/.17f,Mathf.Sin(a)*r),new Vector3(.008f,.004f,.009f),i%3==0?fleck:coffee);}
   Hand=new GameObject("right hand holding grip").transform;Hand.SetParent(Tool,false);
   Cube("palm",Hand,new Vector3(.055f,.12f,-.51f),new Vector3(.19f,.09f,.19f),skin);
   for(int i=0;i<4;i++){Cube("finger over grip",Hand,new Vector3(-.015f,.115f,-.42f-i*.047f),new Vector3(.16f,.055f,.035f),skin);Cube("bent finger",Hand,new Vector3(-.092f,.075f,-.42f-i*.047f),new Vector3(.045f,.075f,.035f),skin);}
   Cube("thumb",Hand,new Vector3(.085f,.158f,-.39f),new Vector3(.065f,.06f,.14f),skin);
   var sleeve=Cube("sleeve",Hand,new Vector3(.28f,.11f,-.7f),new Vector3(.28f,.17f,.28f),dark);sleeve.localEulerAngles=new Vector3(0,-35,0);
   Hand.gameObject.SetActive(false);
   Cube("prep tool rack",root,new Vector3(1.38f,.025f,-.43f),new Vector3(.48f,.05f,1.1f),dark);
   Distributor=new GameObject("surface leveling tool").transform;Distributor.SetParent(root);Distributor.position=DistributorHome;
   Cylinder("leveler metal base",Distributor,new Vector3(0,.025f,0),.19f,.05f,light);
   var levelGrip=Cylinder("leveler grip",Distributor,new Vector3(0,.085f,0),.165f,.08f,dark);levelGrip.gameObject.AddComponent<BoxCollider>().size=new Vector3(.46f,.24f,.46f);
   Cube("leveler rotation stripe",Distributor,new Vector3(0,.13f,0),new Vector3(.25f,.012f,.025f),gold);
   Tamper=new GameObject("hand tamper").transform;Tamper.SetParent(root);Tamper.position=TamperHome;
   Cylinder("tamper flat foot",Tamper,new Vector3(0,.018f,0),.18f,.036f,light);
   Cylinder("tamper stem",Tamper,new Vector3(0,.075f,0),.045f,.1f,gold);
   var knob=Lathe("tamper knob",Tamper,new Vector3(0,.12f,0),new Vector2[]{new Vector2(.045f,0),new Vector2(.08f,.035f),new Vector2(.09f,.115f),new Vector2(.055f,.145f),new Vector2(0,.15f)},dark,16);var knobHit=knob.gameObject.AddComponent<BoxCollider>();knobHit.size=new Vector3(.4f,.25f,.3f);knobHit.center=Vector3.zero;
   PrepHint=Ring("basket placement guide",Tool,new Vector3(0,.165f,0),.28f,.255f,.008f,gold);PrepHint.gameObject.SetActive(false);
   PrepHand=new GameObject("hand on prep tool").transform;PrepHand.SetParent(root);
   Cube("prep palm",PrepHand,new Vector3(.03f,.25f,0),new Vector3(.22f,.08f,.2f),skin);
   for(int i=0;i<4;i++)Cube("prep finger",PrepHand,new Vector3(-.09f,.19f,-.075f+i*.047f),new Vector3(.055f,.13f,.037f),skin);
   Cube("prep thumb",PrepHand,new Vector3(.12f,.18f,.015f),new Vector3(.06f,.12f,.08f),skin);
   Cube("prep sleeve",PrepHand,new Vector3(.26f,.26f,0),new Vector3(.3f,.15f,.22f),dark);PrepHand.gameObject.SetActive(false);
   Shadow=Cylinder("tool contact shadow",root,new Vector3(Mat.x,.055f,Mat.z),.28f,.005f,dark);
   for(int i=0;i<grains.Length;i++){grains[i]=Cube("falling coffee pixel",root,Vector3.zero,new Vector3(.012f,.018f,.012f),i%3==0?fleck:coffee);grains[i].gameObject.SetActive(false);}
   var brewMachine=new GameObject("espresso machine").transform;brewMachine.SetParent(root);brewMachine.position=new Vector3(2.5f,0,.5f);
   Cube("espresso base",brewMachine,new Vector3(0,.065f,-.2f),new Vector3(1.35f,.13f,1.1f),dark);
   Cube("drip tray",brewMachine,new Vector3(0,.14f,-.29f),new Vector3(1.2f,.03f,.82f),steel);
   for(int i=0;i<17;i++)Cube("tray slit",brewMachine,new Vector3(-.55f+i*.068f,.158f,-.3f),new Vector3(.018f,.008f,.7f),dark);
   Cube("espresso rear",brewMachine,new Vector3(0,.81f,.24f),new Vector3(1.25f,1.45f,.3f),steel);
   foreach(float side in new[]{-.53f,.53f})Cube("espresso side pillar",brewMachine,new Vector3(side,.72f,.1f),new Vector3(.17f,1.2f,.42f),steel);
   Cube("espresso boiler",brewMachine,new Vector3(0,1.34f,0),new Vector3(1.35f,.55f,.68f),steel);
   Cube("boiler fascia",brewMachine,new Vector3(0,1.34f,-.35f),new Vector3(1.22f,.38f,.025f),dark);
   Cube("brass fascia trim",brewMachine,new Vector3(0,1.14f,-.37f),new Vector3(1.3f,.025f,.025f),gold);
   Cylinder("group head",root,new Vector3(2.5f,1.025f,.05f),.25f,.11f,light);
   Cylinder("group head underside",root,new Vector3(2.5f,.965f,.05f),.225f,.018f,dark);
   DockHint=Ring("insertion contact guide",root,GroupSeat+Vector3.up*.15f,.29f,.265f,.012f,gold);DockHint.gameObject.SetActive(false);
   var gauge=Cylinder("pressure dial",brewMachine,new Vector3(-.32f,1.35f,-.385f),.11f,.025f,light);gauge.localRotation=Quaternion.Euler(90,0,0);
   Cube("pressure needle",brewMachine,new Vector3(-.32f,1.37f,-.405f),new Vector3(.012f,.1f,.008f),gold);
   BrewButton=Cube("espresso pump button",root,new Vector3(2.83f,1.35f,.11f),new Vector3(.16f,.13f,.05f),gold);BrewButton.gameObject.AddComponent<BoxCollider>();
   Cube("steam wand",brewMachine,new Vector3(.65f,.72f,-.24f),new Vector3(.035f,.65f,.035f),light);
   Cylinder("saucer",root,new Vector3(CupSeat.x,.17f,CupSeat.z),.26f,.025f,light);
   CupHint=Ring("cup seat placement guide",root,new Vector3(CupSeat.x,.19f,CupSeat.z),.31f,.285f,.008f,gold);CupHint.gameObject.SetActive(false);
   Cup=new GameObject("espresso cup").transform;Cup.SetParent(root);Cup.position=CupHome;
   var cupWall=Lathe("cup ceramic shell",Cup,Vector3.zero,new Vector2[]{new Vector2(.11f,0),new Vector2(.16f,.23f),new Vector2(.14f,.23f),new Vector2(.095f,.025f),new Vector2(0,.025f)},light,32);cupWall.gameObject.AddComponent<BoxCollider>();
   Ring("cup bright rim",Cup,new Vector3(0,.232f,0),.163f,.138f,.012f,light);
   var cupHandle=Ring("cup handle",Cup,new Vector3(.19f,.13f,0),.1f,.065f,.035f,light);cupHandle.localRotation=Quaternion.Euler(90,0,0);cupHandle.gameObject.AddComponent<BoxCollider>().size=new Vector3(.26f,.12f,.26f);
   CoffeeSurface=Cylinder("separate espresso surface",Cup,new Vector3(0,.035f,0),.098f,.009f,MatColor("crema","#B67D43"));CoffeeSurface.gameObject.SetActive(false);
   for(int i=0;i<espresso.Length;i++){espresso[i]=Cube("espresso pixel",root,Vector3.zero,new Vector3(.011f,.025f,.011f),i%3==0?fleck:coffee);espresso[i].gameObject.SetActive(false);}
   for(int i=0;i<2;i++){coffeeStreams[i]=Cylinder("continuous espresso thread",root,Vector3.zero,.008f,1,coffee);coffeeStreams[i].gameObject.SetActive(false);}
   var lamp=new GameObject("warm task light").AddComponent<Light>();lamp.type=LightType.Directional;lamp.color=new Color(1,.86f,.68f);lamp.intensity=1.05f;lamp.transform.rotation=Quaternion.Euler(48,-35,0);
   RenderSettings.ambientLight=new Color(.56f,.63f,.76f);QualitySettings.antiAliasing=0;
   Camera=new GameObject("fixed operator camera").AddComponent<Camera>();Camera.tag="MainCamera";Camera.orthographic=true;Camera.orthographicSize=1.55f;Camera.transform.position=new Vector3(0,2.9f,-4.6f);Camera.transform.LookAt(new Vector3(0,.86f,.12f));Camera.backgroundColor=new Color(.12f,.16f,.23f);Camera.clearFlags=CameraClearFlags.SolidColor;Camera.nearClipPlane=.05f;Camera.farClipPlane=30;
   Pixels=new RenderTexture(480,270,24,RenderTextureFormat.ARGB32){filterMode=FilterMode.Point,antiAliasing=1};Pixels.Create();Camera.targetTexture=Pixels;Camera.aspect=480f/270;
  }
  public void Update(GrindModel m,PuckPrepModel prep,float time){
   Grounds.gameObject.SetActive(m.Grounds>.01f);float height=Mathf.Lerp(.08f,.87f,m.Grounds/18);if(prep.Leveled)height=.65f;if(prep.Complete)height=.37f;else if(prep.Depth>0)height=Mathf.Lerp(.65f,.37f,prep.Depth);Grounds.localScale=new Vector3(1,height,1);
   for(int i=0;i<grains.Length;i++)grains[i].gameObject.SetActive(false);
   int n=0;foreach(var p in m.Parcels){for(int j=0;j<3&&n<grains.Length;j++){var g=grains[n++];g.gameObject.SetActive(true);float t=Mathf.Clamp01(p.Age/GrindModel.Travel);g.position=Vector3.Lerp(Outlet,Tool.TransformPoint(new Vector3(0,.09f,0)),t*t)+new Vector3(Mathf.Sin(n*4.7f)*.035f,0,Mathf.Cos(n*2.3f)*.025f);}}
   Shadow.position=new Vector3(Tool.position.x,.055f,Tool.position.z);Shadow.localScale=new Vector3(.56f, .005f,.56f)*(m.Slot==CraftSlot.Held?1.2f:1);
  }
  public void SetDockHint(bool show,bool ready,bool near){DockHint.gameObject.SetActive(show);if(show){DockHint.GetComponent<Renderer>().sharedMaterial=ready?(near?light:gold):steel;DockHint.localScale=Vector3.one*(near?1.08f:1);}}
  public void SetPrepHint(bool show,bool near){PrepHint.gameObject.SetActive(show);if(show)PrepHint.GetComponent<Renderer>().sharedMaterial=near?gold:steel;}
  public void SetCupHint(bool show,bool near){CupHint.gameObject.SetActive(show);if(show)CupHint.GetComponent<Renderer>().sharedMaterial=near?light:gold;}
  public void UpdateBrew(BrewModel b){
   CoffeeSurface.gameObject.SetActive(b.InCup>.001f);float fill=Mathf.Clamp01(b.InCup/60);CoffeeSurface.localPosition=new Vector3(0,.035f+.25f*fill,0);float radius=Mathf.Lerp(.098f,.136f,fill);CoffeeSurface.localScale=new Vector3(radius/.098f,1,radius/.098f);
   for(int i=0;i<espresso.Length;i++)espresso[i].gameObject.SetActive(false);
   for(int i=0;i<2;i++){var stream=coffeeStreams[i];stream.gameObject.SetActive(b.Running&&b.Pumped>1.5f&&b.InCup>0);float side=i==0?-.11f:.11f;Vector3 from=Tool.TransformPoint(new Vector3(side,-.078f,0)),to=Cup.TransformPoint(new Vector3(side*.8f,CoffeeSurface.localPosition.y,0));stream.position=(from+to)/2;stream.rotation=Quaternion.FromToRotation(Vector3.up,from-to);stream.localScale=new Vector3(1,Vector3.Distance(from,to),1);}
   int n=0;foreach(var p in b.Parcels){float t=Mathf.Clamp01(p.Age/BrewModel.Travel);foreach(float side in new[]{-.11f,.11f}){if(n>=espresso.Length)break;var e=espresso[n++];e.gameObject.SetActive(true);Vector3 from=Tool.TransformPoint(new Vector3(side,-.078f,0));Vector3 to=Cup.TransformPoint(new Vector3(side*.8f,CoffeeSurface.localPosition.y,0));e.position=Vector3.Lerp(from,to,t*t);e.localScale=new Vector3(.012f,.035f+.06f*t,.012f);}}
  }
  Transform Cube(string name,Transform parent,Vector3 p,Vector3 s,Material mat){var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;Object.Destroy(o.GetComponent<Collider>());o.transform.SetParent(parent,false);o.transform.localPosition=p;o.transform.localScale=s;o.GetComponent<Renderer>().sharedMaterial=mat;return o.transform;}
  Transform Cylinder(string name,Transform parent,Vector3 p,float r,float h,Material mat){return Lathe(name,parent,p,new Vector2[]{new Vector2(0,-h/2),new Vector2(r,-h/2),new Vector2(r,h/2),new Vector2(0,h/2)},mat,32);}
  Transform Ring(string name,Transform parent,Vector3 p,float outer,float inner,float h,Material mat){return Lathe(name,parent,p,new Vector2[]{new Vector2(inner,0),new Vector2(outer,0),new Vector2(outer,h),new Vector2(inner,h),new Vector2(inner,0)},mat,32);}
  Transform Lathe(string name,Transform parent,Vector3 p,Vector2[] profile,Material mat,int segments){
   var vs=new System.Collections.Generic.List<Vector3>();var tris=new System.Collections.Generic.List<int>();
   for(int row=0;row<profile.Length;row++)for(int i=0;i<=segments;i++){float a=i*2*Mathf.PI/segments;vs.Add(new Vector3(Mathf.Cos(a)*profile[row].x,profile[row].y,Mathf.Sin(a)*profile[row].x));}
   for(int row=0;row<profile.Length-1;row++)for(int i=0;i<segments;i++){int a=row*(segments+1)+i,b=a+segments+1;tris.Add(a);tris.Add(b);tris.Add(a+1);tris.Add(a+1);tris.Add(b);tris.Add(b+1);}
   var mesh=new Mesh();mesh.SetVertices(vs);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();var o=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));o.transform.SetParent(parent,false);o.transform.localPosition=p;o.GetComponent<MeshFilter>().sharedMesh=mesh;o.GetComponent<MeshRenderer>().sharedMaterial=mat;return o.transform;
  }
 }
}
