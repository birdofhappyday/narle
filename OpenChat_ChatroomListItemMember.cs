using Assets.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Assets.Scripts.UI.Windows.Lobby.OpenChat.OpenChat_ChatroomListItem;

namespace Assets.Scripts.UI.Windows.Lobby.OpenChat
{
    public class OpenChat_ChatroomListItemMember : HM_ScrollObject
    {
        //public UITexture uiText;
        //public GameObject noneGameObject;

        //public override void Draw(object data, poolingInfo drawData)
        //{
        //    var list = GetReference<List<MemberImage>>(data);

        //    try
        //    {
        //        MemberImage ri = list[drawData.dataIndex];

        //        if(!ri.mActive)
        //        {
        //            noneGameObject.SetActive(true);
        //            uiText.gameObject.SetActive(false);
        //            uiText.material = null;
        //            uiText.mainTexture = null;
        //        }
        //        else
        //        {
        //            noneGameObject.SetActive(false);
        //            uiText.gameObject.SetActive(true);
        //            //uiText.mainTexture = ri.mImage;
        //            uiText.material = UIUtil.SetProfilePhotoMaterial(ri.mImage);
        //        }
        //    }
        //    catch
        //    {
        //        Debug.LogError("오픈 채팅방 멤버 정보 표시하는데 실패했습니다.");
        //    }
        //}

        //public void SetData(bool active, Texture2D texture2D = null)
        //{
        //    if (!active)
        //    {
        //        noneGameObject.SetActive(true);
        //        uiText.gameObject.SetActive(false);
        //        uiText.material = null;
        //        uiText.mainTexture = null;
        //    }
        //    else
        //    {
        //        noneGameObject.SetActive(false);
        //        uiText.gameObject.SetActive(true);
        //        //uiText.mainTexture = texture2D;
        //        uiText.material = UIUtil.SetProfilePhotoMaterial(texture2D);
        //    }
        //}

        //void OnDrag(Vector2 delta)
        //{
        //    var wnd = UIUtil.GetWnd<OpenChatWnd>();
        //    wnd.swipeActive = true;
        //}
    }
}
