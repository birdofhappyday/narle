using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Scripts.UI.Windows.Lobby.OpenChat
{
    public class OpenChat_tag : HM_ScrollObject
    {
        // [gh041606] 정보 검색용.
        private string realTag = "";
        public UILabel      text;
        public UISprite     sprite;

        private void OnMemDragStart()
        {
            UIManager.isSwipeStop = true;
        }

        private void OnMemDragEnd()
        {
            UIManager.isSwipeStop = false;
        }
        //

        public override void Draw(object data, poolingInfo drawData)
        {
            var list = GetReference<List<OpenChatTaginfo>>(data);

            try
            {
                OpenChatTaginfo ri = list[drawData.dataIndex];
                text.text = ri.tagName;
                realTag = ri.tagName;
                // [gh041605] 글자 수 15보다 많으면 ...
                if (text.text.Length > 15)
                {
                    text.text = text.text.Substring(0, 15) + "...";
                }
            }
            catch
            {
                Debug.LogError("오픈 태그정보 표시하는데 실패했습니다.");
            }
        }

        public void SetData(string uiText)
        {
            realTag = uiText;
            text.text = uiText;
            // [gh041605] 글자 수 15보다 많으면 ...
            if (text.text.Length > 15)
            {
                text.text = text.text.Substring(0, 15) + "...";
            }
            sprite.SetAnchor(text.transform);
            sprite.updateAnchors = UIRect.AnchorUpdate.OnUpdate;

            sprite.ResetAnchors();

            //[gh041609] 스와이프 방지.
            UIScrollView scrollView = transform.parent.parent.GetComponent<UIScrollView>();
            scrollView.onDragStarted += OnMemDragStart;
            scrollView.onDragFinished += OnMemDragEnd;
        }

        /// <summary>
        /// Tag 클릭.
        /// </summary>
        public void OnClick_SearchTagButton()
        {
            var OpenChatWnd = UIManager.instance.GetWnd<OpenChatWnd>();

            Debug.Log("OnClick_TagSearchButton 버튼 클릭");
            if (null != OpenChatWnd)
            {
                OpenChatWnd.TagSearch(realTag);
            }
        }
    }
}
