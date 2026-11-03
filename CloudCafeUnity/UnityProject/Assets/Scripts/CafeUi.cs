using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace CloudCafe {
 public sealed class CafeUi:MonoBehaviour {
  public Button DialogueNext {get;private set;}
  public Button OrderButton {get;private set;}
  public Button HoldButton {get;private set;}
  public Button FinishButton {get;private set;}
  public Button ServeButton {get;private set;}
  public Button RetryButton {get;private set;}
  public bool HelpOpen=>helpPanel!=null&&helpPanel.activeSelf;
  public Canvas Canvas {get;private set;}
  CafeController owner;Font font;Transform root;
  Sprite navyPanel,paperPanel,buttonPanel;readonly List<Texture2D> textures=new List<Texture2D>();readonly List<Sprite> generatedSprites=new List<Sprite>();
  readonly List<RaycastResult> raycasts=new List<RaycastResult>();
  readonly Color navy=Hex("1E2941"),cream=Hex("F3E7CC"),gold=Hex("BDA36E"),ink=Hex("30384A"),muted=Hex("AAB5C9"),green=Hex("9DBEAC");
  Text dayText,coinText,speaker,dialogueText,nextText,memoText,metric,secondary,instruction,holdLabel,stageTitle,resetLabel;
  GameObject dialogue,order,memo,steps,recipe,arrival,helpPanel;
  Image meterFill;Image[] stepPanels=new Image[3];Text[] stepLabels=new Text[3];
  bool resetConfirm;
  RawImage milkImage;
  public void SetMilkTexture(RenderTexture texture){var t=RectObject("Physical milk - original art textures",root,new Rect(20,64,440,270));milkImage=t.gameObject.AddComponent<RawImage>();milkImage.texture=texture;milkImage.raycastTarget=false;t.SetAsFirstSibling();milkImage.gameObject.SetActive(false);}
  static Color Hex(string hex){Color c;ColorUtility.TryParseHtmlString("#"+hex,out c);return c;}
  public void Initialize(CafeController controller,Camera camera){
   owner=controller;
   font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},13);
   var go=new GameObject("UI - shared panels, live text and buttons",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
   Canvas=go.GetComponent<Canvas>();Canvas.renderMode=RenderMode.WorldSpace;Canvas.worldCamera=camera;Canvas.sortingOrder=1000;
   var canvasRect=(RectTransform)go.transform;canvasRect.pivot=new Vector2(0,1);canvasRect.sizeDelta=new Vector2(640,360);go.transform.localScale=Vector3.one*.01f;go.transform.position=new Vector3(-3.2f,1.8f,-5);
   var scaler=go.GetComponent<CanvasScaler>();scaler.dynamicPixelsPerUnit=1;scaler.referencePixelsPerUnit=100;root=go.transform;
   if(EventSystem.current==null)new GameObject("UI Event System",typeof(EventSystem),typeof(StandaloneInputModule));
   EventSystem.current.sendNavigationEvents=false; // Enter/Space are handled by the same game commands.
   navyPanel=PanelSprite(navy,gold);paperPanel=PanelSprite(cream,Hex("AC9470"));buttonPanel=PanelSprite(Hex("293750"),Hex("718098"));
   var day=Panel("Day",root,new Rect(10,10,91,21),navyPanel);Icon(day.transform,"sun",4,3,15);dayText=Text("Day text",day.transform,new Rect(23,2,64,17),"",10,cream);
   var coins=Panel("Coins",root,new Rect(564,10,66,21),navyPanel);Icon(coins.transform,"coin",5,4,13);coinText=Text("Coin text",coins.transform,new Rect(22,2,38,17),"",11,cream,TextAnchor.MiddleRight);
   Button("Help",root,new Rect(538,10,20,21),"?",ToggleHelp,10);
   dialogue=Panel("Dialogue",root,new Rect(112,304,416,48),navyPanel).gameObject;
   speaker=Text("Speaker",dialogue.transform,new Rect(12,4,330,13),"사신",10,gold);
   dialogueText=Text("Live dialogue",dialogue.transform,new Rect(12,21,348,22),"",13,cream);
   DialogueNext=Button("Next dialogue",dialogue.transform,new Rect(374,13,30,25),"›",owner.Next,16);
   nextText=DialogueNext.GetComponentInChildren<Text>();
   arrival=Panel("Arrival hint",root,new Rect(224,325,192,22),navyPanel).gameObject;Text("Arrival text",arrival.transform,new Rect(7,2,178,18),"구름 사이로 손님이 찾아옵니다…",10,cream,TextAnchor.MiddleCenter);
   memo=Panel("Small folded order",root,new Rect(510,233,118,37),paperPanel).gameObject;memoText=Text("Memo text",memo.transform,new Rect(8,5,102,27),"주문 메모\n따뜻한 라테",10,ink);
   order=Panel("Order note",root,new Rect(482,151,146,105),paperPanel).gameObject;
   Text("Order heading",order.transform,new Rect(10,6,100,15),"주문 메모",10,Hex("796B54"));
   Text("Drink name",order.transform,new Rect(10,25,120,21),"따뜻한 라테",14,ink);
   Text("Ingredients",order.transform,new Rect(10,50,125,16),"커피 + 우유 150 ml",10,ink);
   OrderButton=Button("Make latte",order.transform,new Rect(10,74,126,23),"만들기",owner.Next,12);
   steps=Panel("Three preparation steps",root,new Rect(164,10,288,34),navyPanel).gameObject;
   for(int i=0;i<3;i++){
    var tab=Panel("Step "+i,steps.transform,new Rect(3+i*95,3,92,28),buttonPanel);tab.raycastTarget=false;stepPanels[i]=tab;
    Icon(tab.transform,i==0?"beans":i==1?"cup":"pitcher",4,2,24);
    stepLabels[i]=Text("Step name",tab.transform,new Rect(33,3,55,22),(i+1)+" "+new[]{"분쇄","추출","우유"}[i],10,cream,TextAnchor.MiddleLeft);
   }
   // Fixed slots: the panel never resizes and every action keeps its place across grind, extract, pour and serve.
   recipe=Panel("Preparation information",root,new Rect(472,70,156,258),paperPanel).gameObject;
   Text("Recipe heading",recipe.transform,new Rect(12,8,132,15),"오늘의 주문",10,Hex("796B54"));
   Text("Recipe name",recipe.transform,new Rect(12,27,132,22),"따뜻한 라테",14,ink);
   Line(recipe.transform,new Rect(12,57,132,1),Hex("C7B796"));
   stageTitle=Text("Current step",recipe.transform,new Rect(12,68,132,19),"",12,ink);
   metric=Text("Current and target",recipe.transform,new Rect(12,91,132,25),"",17,ink);
   secondary=Text("Secondary value",recipe.transform,new Rect(12,121,132,17),"",9,Hex("796B54"));
   Line(recipe.transform,new Rect(12,144,132,5),Hex("D6C9AF"));meterFill=Line(recipe.transform,new Rect(12,144,0,5),green);
   instruction=Text("Instruction - two lines max",recipe.transform,new Rect(12,156,132,30),"",10,ink);
   HoldButton=Button("Hold current action",recipe.transform,new Rect(12,194,132,26),"",()=>{},12);holdLabel=HoldButton.GetComponentInChildren<Text>();
   HoldButton.gameObject.AddComponent<CafeHoldButton>().Changed=owner.SetUiHeld;
   ServeButton=Button("Serve latte",recipe.transform,new Rect(12,194,132,26),"손님에게 건네기",owner.Next,11);
   // Secondary row: finish stays visible but disabled until it applies; retry always shares the row.
   FinishButton=Button("Finish pouring",recipe.transform,new Rect(12,226,64,22),"붓기 마침",owner.FinishPour,10);
   RetryButton=Button("Retry drink",recipe.transform,new Rect(80,226,64,22),"다시 만들기",owner.RetryDrink,10);
   helpPanel=Panel("Help and save settings",root,new Rect(204,89,232,182),navyPanel).gameObject;
   Text("Help title",helpPanel.transform,new Rect(14,10,204,22),"잠깐 쉬어가기",14,cream);
   Text("Controls",helpPanel.transform,new Rect(14,40,204,74),"Space  누르기 / 떼기\nEnter  다음 대화\n피처를 누르고 아래로 끌어 붓기\n완료한 날과 보상은 자동 저장",11,cream);
   var reset=Button("Reset save",helpPanel.transform,new Rect(14,122,204,22),"저장 초기화",()=>{if(resetConfirm){owner.ResetSave();resetConfirm=false;ToggleHelp();}else{resetConfirm=true;resetLabel.text="초기화하려면 한 번 더 클릭";}},10);resetLabel=reset.GetComponentInChildren<Text>();
   Button("Close help",helpPanel.transform,new Rect(14,150,204,22),"계속하기",ToggleHelp,11);helpPanel.SetActive(false);
   Refresh();
  }
  Sprite PanelSprite(Color fill,Color border){
   var texture=new Texture2D(12,12,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;var pixels=new Color[144];
   for(int y=0;y<12;y++)for(int x=0;x<12;x++){bool edge=x==0||x==11||y==0||y==11;pixels[y*12+x]=edge?border:fill;if((x==0||x==11)&&(y==0||y==11))pixels[y*12+x]=Color.clear;}
   texture.SetPixels(pixels);texture.Apply();textures.Add(texture);var sprite=Sprite.Create(texture,new Rect(0,0,12,12),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(2,2,2,2));generatedSprites.Add(sprite);return sprite;
  }
  RectTransform RectObject(string name,Transform parent,Rect bounds){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var t=(RectTransform)go.transform;t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(bounds.x,-bounds.y);t.sizeDelta=bounds.size;return t;}
  Image Panel(string name,Transform parent,Rect bounds,Sprite sprite){var t=RectObject(name,parent,bounds);var image=t.gameObject.AddComponent<Image>();image.sprite=sprite;image.type=Image.Type.Sliced;image.color=Color.white;return image;}
  Image Line(Transform parent,Rect bounds,Color color){var t=RectObject("Rule / meter",parent,bounds);var image=t.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
  Text Text(string name,Transform parent,Rect bounds,string value,int size,Color color,TextAnchor align=TextAnchor.UpperLeft){var t=RectObject(name,parent,bounds);var text=t.gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.fontStyle=FontStyle.Normal;text.text=value;text.color=color;text.alignment=align;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.supportRichText=false;text.raycastTarget=false;return text;}
  Button Button(string name,Transform parent,Rect bounds,string caption,UnityEngine.Events.UnityAction click,int size){var image=Panel(name,parent,bounds,buttonPanel);var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.16f,1.16f,1.13f);colors.pressedColor=new Color(.67f,.72f,.80f);colors.disabledColor=new Color(.46f,.49f,.54f);colors.fadeDuration=.06f;button.colors=colors;button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(click);Text("Label",image.transform,new Rect(2,0,bounds.width-4,bounds.height),caption,size,cream,TextAnchor.MiddleCenter);return button;}
  void Icon(Transform parent,string kind,float x,float y,int size){
   var tex=new Texture2D(24,24,TextureFormat.RGBA32,false);tex.filterMode=FilterMode.Point;var p=new Color[576];Color edge=Hex("738397"),light=cream,shade=Hex("C5AF86");
   Action<int,int,int,int,Color> box=(a,b,w,h,c)=>{for(int yy=b;yy<b+h;yy++)for(int xx=a;xx<a+w;xx++)if(xx>=0&&xx<24&&yy>=0&&yy<24)p[(23-yy)*24+xx]=c;};
   if(kind=="beans"){for(int k=0;k<3;k++){int cx=k==0?8:k==1?16:11,cy=k==0?7:k==1?11:17;for(int yy=-4;yy<=4;yy++)for(int xx=-3;xx<=3;xx++)if(xx*xx/9f+yy*yy/16f<=1){box(cx+xx,cy+yy,1,1,light);if(xx==0)box(cx+xx,cy+yy,1,1,shade);}}}
   else if(kind=="cup"){box(4,7,13,11,edge);box(5,8,11,8,light);box(6,9,9,2,shade);box(17,8,4,7,light);box(17,10,2,3,navy);box(3,19,17,1,light);}
   else if(kind=="pitcher"){box(7,5,10,15,edge);box(8,6,8,13,light);box(5,5,4,3,light);box(17,7,4,10,light);box(17,9,2,6,navy);box(10,8,1,9,shade);}
   else {for(int yy=0;yy<24;yy++)for(int xx=0;xx<24;xx++){float d=(xx-11.5f)*(xx-11.5f)+(yy-11.5f)*(yy-11.5f);if(d<49)p[(23-yy)*24+xx]=kind=="coin"?gold:light;}if(kind=="sun"){box(11,1,2,2,light);box(11,21,2,2,light);box(1,11,2,2,light);box(21,11,2,2,light);}else {box(11,7,2,10,shade);}}
   tex.SetPixels(p);tex.Apply();textures.Add(tex);var sprite=Sprite.Create(tex,new Rect(0,0,24,24),new Vector2(.5f,.5f),100);generatedSprites.Add(sprite);var image=Panel("Icon "+kind,parent,new Rect(x,y,size,size),sprite);image.type=Image.Type.Simple;image.raycastTarget=false;
  }
  public bool BlocksWorld(Vector2 screen){if(EventSystem.current==null)return false;raycasts.Clear();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},raycasts);return raycasts.Count>0;}
  public void ToggleHelp(){helpPanel.SetActive(!helpPanel.activeSelf);resetConfirm=false;resetLabel.text="저장 초기화";owner.SetUiHeld(false);}
  public void Refresh(){
   var s=owner.State;var u=CafeUiRules.For(s);dayText.text=s.Day+"일차 · 오후";coinText.text=s.Coins.ToString();
   if(milkImage)milkImage.gameObject.SetActive(s.Stage==Phase.Milk||s.Stage==Phase.Ready);
   dialogue.SetActive(u.Dialogue);order.SetActive(u.Order);recipe.SetActive(u.Steps);steps.SetActive(u.Steps);arrival.SetActive(s.Stage==Phase.Arrival);
   memo.SetActive(s.Stage==Phase.Dialogue&&s.Line>=3);memoText.text="주문 메모\n따뜻한 라테";
   speaker.text=s.Stage==Phase.Done?"영업 종료":s.Stage==Phase.Dialogue&&s.Line==1?"나":s.Stage==Phase.Reaction?"사신    완성도 "+s.Score+" · +"+s.Reward+" 코인":"사신";
   dialogueText.text=owner.DialogueBody();DialogueNext.gameObject.SetActive(s.Stage!=Phase.Order);nextText.text=s.Stage==Phase.Done?"내일":s.Stage==Phase.Reaction?"끝":"›";nextText.fontSize=s.Stage==Phase.Dialogue?16:10;
   HoldButton.gameObject.SetActive(u.Hold);FinishButton.gameObject.SetActive(u.Steps);FinishButton.interactable=u.Finish&&u.CanFinish;ServeButton.gameObject.SetActive(u.Serve);RetryButton.gameObject.SetActive(u.Retry);
   if(s.Stage==Phase.Milk&&owner.MilkStation!=null)FinishButton.interactable&=owner.MilkStation.CanFinish;
   for(int i=0;i<3;i++){bool active=i==u.ActiveStep;stepPanels[i].sprite=active?navyPanel:buttonPanel;stepPanels[i].color=active?Color.white:new Color(.64f,.69f,.77f);stepLabels[i].color=i<u.ActiveStep?green:active?cream:muted;}
   float progress=0;
   if(s.Stage==Phase.Grind){stageTitle.text="원두 분쇄";metric.text=Mathf.RoundToInt(s.Ground*100)+" / 100 %";secondary.text="라테 한 잔 분량";instruction.text="누르고 있으면\n원두가 갈려요.";holdLabel.text="누르고 있기 · 분쇄";progress=s.Ground;}
   else if(s.Stage==Phase.Extract){stageTitle.text="에스프레소 추출";metric.text=s.ExtractTime.ToString("F1")+" / 3.0 초";secondary.text="3초 근처에서 떼기";instruction.text="누르면 추출 시작.\n3초쯤 손을 떼세요.";holdLabel.text="누른 뒤 떼기 · 추출";progress=s.ExtractTime/3;}
   else if(s.Stage==Phase.Milk||s.Stage==Phase.Ready){stageTitle.text=s.Stage==Phase.Ready?"제공 준비 완료":"우유 붓기";metric.text=Mathf.RoundToInt(s.Milk)+" / 150 ml";secondary.text="피처 "+Mathf.RoundToInt(s.Remaining)+" ml · 흘림 "+Mathf.RoundToInt(s.Spill)+" ml";instruction.text=s.Stage==Phase.Ready?"라테가 완성됐어요.\n손님에게 건네 주세요.":s.InFlight>=.5f&&owner.MilkStation.Angle<2?"마지막 방울이\n컵에 닿기를 기다려요.":"누르고 아래로 끌어 붓기.\n놓으면 바로 세워요.";holdLabel.text="누르고 있기 · 붓기";progress=s.Milk/150;}
   meterFill.rectTransform.sizeDelta=new Vector2(Mathf.Round(132*Mathf.Clamp01(progress)),5);meterFill.color=s.Stage==Phase.Extract&&s.ExtractTime>3.4f?Hex("BE8B75"):green;
   if(font.material.mainTexture!=null)font.material.mainTexture.filterMode=FilterMode.Point;
  }
  void OnDestroy(){foreach(var sprite in generatedSprites)if(sprite)Destroy(sprite);foreach(var texture in textures)if(texture)Destroy(texture);if(font)Destroy(font);}
 }
}
