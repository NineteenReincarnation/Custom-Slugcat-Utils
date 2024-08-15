using System.Linq;
using CustomSlugcatUtils.Hooks;
using CustomSlugcatUtils.Objects;
using DevInterface;
using RWCustom;
using UnityEngine;

namespace CustomSlugcatUtils.Dev
{
    internal class CustomWhiteTokenRepresentation : ResizeableObjectRepresentation
    {
       
        public CustomWhiteTokenRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj) : base(owner, IDstring, parentNode, pObj, "Custom ChatLog Token", false)
        {
            this.subNodes.Add(new TokenControlPanel(owner, "Token_Panel", this, new Vector2(0f, 100f)));
            (this.subNodes[this.subNodes.Count - 1] as TokenControlPanel).pos = (pObj.data as CustomWhiteToken.CollectTokenData).panelPos;
            this.fSprites.Add(new FSprite("pixel"));
            this.lineSprite = this.fSprites.Count - 1;
            owner.placedObjectsContainer.AddChild(this.fSprites[this.lineSprite]);
            this.fSprites[this.lineSprite].anchorY = 0f;
        }


        public override void Refresh()
        {
            base.Refresh();
            base.MoveSprite(this.lineSprite, this.absPos);
            this.fSprites[this.lineSprite].scaleY = (this.subNodes[1] as TokenControlPanel).pos.magnitude;
            this.fSprites[this.lineSprite].rotation = Custom.AimFromOneVectorToAnother(this.absPos, (this.subNodes[1] as CustomWhiteTokenRepresentation.TokenControlPanel).absPos);
            (this.pObj.data as CustomWhiteToken.CollectTokenData).panelPos = (this.subNodes[1] as Panel).pos;
        }

        private readonly int lineSprite;


        internal class TokenControlPanel : Panel , IDevUISignals
        {
    
            public CustomWhiteToken.CollectTokenData TokenData => (this.parentNode as CustomWhiteTokenRepresentation).pObj.data as CustomWhiteToken.CollectTokenData;

           
            public TokenControlPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(150f, 30f), "Custom Chatlog Token")
            {
                lbl = new Button(owner, "Token_Button", this, new Vector2(5f, 5f), 140f, "");
                subNodes.Add(lbl);
                UpdateTokenText();
            }


            
            private void UpdateTokenText()
            {
                lbl.Text = string.IsNullOrEmpty(TokenData.chatlogId) ? "Null ChatLog" : TokenData.chatlogId;
            }

            
            public DevUILabel lbl;
            private SelectChatlogPanel panel;

            public void Signal(DevUISignalType type, DevUINode sender, string message)
            {
                if (sender.IDstring == "Token_Button" && panel == null)
                {
                    subNodes.Add(panel = new SelectChatlogPanel(owner, this, pos + new Vector2(200, -50)));

                }
            }

            public void SelectChatlogID(string id)
            {
                TokenData.chatlogId = id;
                subNodes.Remove(panel);
                panel.ClearSprites();
                UpdateTokenText();

                panel = null;
            }
        }


        internal class SelectChatlogPanel : Panel, IDevUISignals
        {
            public SelectChatlogPanel(DevUI owner, DevUINode parentNode, Vector2 pos) : base(owner, "Select_Chatlog", parentNode,pos, new Vector2(200f, 220f), "Select Chatlog")
            {
                for (int j = 0; j < 2; j++)
                    subNodes.Add(new Button(owner, (j != 0) ? "Next_Button" : "Prev_Button", this, new Vector2(5f + 100f * (float)j, this.size.y - 16f - 5f), 95f, (j != 0) ? "Next Page" : "Previous Page"));
                maxCount = ChatLogHooks.PathMaps.Count;
                maxPage = Mathf.CeilToInt(maxCount / (float)MaxButtonsPerLine);
                RefreshButtons();
            }

            public const int MaxButtonsPerLine = 10;

            private readonly int maxPage;
            private readonly int maxCount;
            private int pageIndex = 0;

            public void RefreshButtons()
            {
                foreach (var button in subNodes.Where(i => i.IDstring.Contains("Select")))
                {
                    button.ClearSprites();
                    subNodes.Remove(button);
                }

                var startIndex = pageIndex * MaxButtonsPerLine;

                for (int i = 0; i < MaxButtonsPerLine; i++)
                {
                    if (maxCount <= startIndex + i)
                        return;
                    subNodes.Add(new Button(owner,
                        $"Select_{ChatLogHooks.PathMaps.Keys.ToArray()[i + startIndex]}", this,
                        new Vector2(5f + 100f, size.y - 16f - 35f - 20f * (float)i), 190f,
                        $"{ChatLogHooks.PathMaps.Keys.ToArray()[i + startIndex]}"));
                }

            }

            public void Signal(DevUISignalType type, DevUINode sender, string message)
            {
                string idstring = sender.IDstring;
                if (idstring.StartsWith("Select_"))
                {
                    (parentNode as TokenControlPanel).SelectChatlogID(idstring.Replace("Select_",""));
                    return;
                }
                switch (idstring)
                {
                    case "Prev_Button":
                        pageIndex = (pageIndex - 1 + maxPage) % maxPage;
                        RefreshButtons();
                        break;
                    case "Next_Button":
                        pageIndex = (pageIndex + 1) % maxPage;
                        RefreshButtons();
                        break;
                }
            }
        }
    }

}
