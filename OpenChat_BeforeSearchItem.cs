using System;
using System.Collections.Generic;
using Assets.Scripts.Utils;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.UI.Windows.Lobby.OpenChatWnd;

namespace Assets.Scripts.UI.Windows.Lobby.OpenChat
{
    public class OpenChat_BeforeSearchItem : HM_ScrollObject
    {
        public UILabel searchText;
        public UILabel dateTime;

        public override void Draw(object data, poolingInfo drawData)
        {
            var list = GetReference<List<BeforeSearchInputString>>(data);

            try
            {
                BeforeSearchInputString ri = list[drawData.dataIndex];
                searchText.text = ri.msearchString;
                // [gh041605] 글자 수 15보다 많으면 ...
                if (searchText.text.Length > 30)
                {
                    searchText.text = searchText.text.Substring(0, 30) + "...";
                }
                dateTime.text = ri.msearchDate;
            }
            catch
            {
                Debug.LogError("오픈 태그정보 표시하는데 실패했습니다.");
            }
        }

        public void RemoveWord()
        {
            var wnd = UIUtil.GetWnd<OpenChatWnd>();
            wnd.RemoveBeforeOpenChatRoomSearchWord(searchText.text);
        }

        public void BeforeWordSearch()
        {
            var wnd = UIUtil.GetWnd<OpenChatWnd>();
            wnd.WordSearch(searchText.text);
        }
    }
}
