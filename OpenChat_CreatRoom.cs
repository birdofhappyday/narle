using System;
using System.Collections.Generic;
using Assets.Scripts.Utils;
using System.Linq;
using System.Text;
using Assets.Scripts.UI.Windows.Lobby;
using Assets.Scripts.User;
using UnityEngine;
using Assets.Scripts.UI.Windows.Lobby.VideoCallWindow;
using Assets.Scripts.Messenger;
using Assets.Scripts.Networks.Web;
using System.Collections;
using Assets.Scripts.Cores;
using System.Text.RegularExpressions;

namespace Assets.Scripts.UI.Windows.Lobby.OpenChat
{
    public class OpenChat_CreatRoom : UISwipeWndBase
    {
        public UIInput OpenChatRoomTitleInput;
        public UIInput OpenChatRoomTagInput;
        public UILabel[] UItext;
        public string[] LimitWords;

        private int MaxPeopleNumber = 8;

        public override void Open(params object[] args)
        {
            var wnd = UIUtil.GetWnd<VideoCallWnd>(false);
            if (wnd != null)
            {
                if (wnd.callStarted)
                {
                    UIUtil.OpenWnd(eWindow.POP_ErrorMessage, "통화 중에는 오픈플래닛을 만들 수 없습니다.");
                    Close();
                    return;
                }
            }

            string input = string.Empty;
            if (args != null && args.Length == 1)
            {
                input = (string)args[0];
            }

            UIUtil.CloseWnd<LobbyMainWnd>();
            Init(input);
            base.Open(args);
        }

        public override void Close(params object[] args)
        {
            base.Close(args);
            UIUtil.OpenWnd<LobbyMainWnd>();
            // [gh061101] 로그 기록.
            PanalyzerUtil.PAN_menuItem("DH", "C", "");
        }

        public void Init(string input = null)
        {
            OpenChatRoomTitleInput.value = input;
            if (input.IsEmpty())
            {
                UItext[0].text = "플래닛 이름을 입력하세요.";
            }
            else
            {
                UItext[0].text = input;
            }

            // [gh041009] max 6 -> 8 로 변경.
            OpenChatRoomTagInput.value = "";
            // UItext[1].text = "";
            UItext[1].text = "#방탄#수다#서울";
            UItext[2].text = "8";
            MaxPeopleNumber = 8;
        }

        /// <summary>
        /// 금칙어 단어 비교.
        /// 나중에 수정 필요가 있다.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool CheckOpenChatRoomName(string text)
        {
            string s2 = text.ToLower();

            if (!string.IsNullOrEmpty(s2))
            {
                foreach (var it in LimitWords)
                {
                    if (s2.Contains(it))
                        return false;
                }
            }

            return true;
        }

        // [gh052006] submit 처리.
        public void OnInputSubmit(UIInput input)
        {
            if(input.value.Equals("#"))
            {
                input.value = "";
            }
        }

        // [gh052005] 클릭시 # 확인.
        public void OnClickInput(UIInput input)
        {
            // [gh052103] 엔터 처리 및 utf16처리.
            if (Input.GetKey(KeyCode.Return))
            {
                return;
            }
            if (input.value.Length == 0)
            {
                input.value = "#";
            }
            else
            {
                input.value = Regex.Replace(input.value, @"\p{Cs}", "");
            }
        }

        // [gh052104] 제목 입력시 체크.
        public void OnClickInputTitle(UIInput input)
        {
            // [gh052103] 엔터 처리 및 utf16처리.
            if (Input.GetKey(KeyCode.Return))
            {
                return;
            }
            input.value = Regex.Replace(input.value, @"\p{Cs}", "");
        }

        // [gh041809] 띄어쓰기도 샵처리. -> gh042204 summit으로 변경.
        // [gh052003] 복구 요청됨.
        public void CheckSpace(UIInput input)
        {
            if (input.value.Length == 0)
            {
                return;
            }
            else if (input.value.Length == 1)
            {
                if (!input.value.Equals("#"))
                {
                    input.value = "#" + input.value;
                }
            }
            else
            {
                if (Input.GetKey(KeyCode.Return))
                {
                    return;
                }
                input.value = Regex.Replace(input.value, @"\p{Cs}", "");
                input.value = input.value.Replace(' ', '#');
            }
        }

        /// <summary>
        /// 완료 버튼 클릭.
        /// </summary>
        public void Onclick_MakeOpenChatRoom()
        {
            // [gh042408] 오버레이 권한 체크.
            if (!ScriptMgr.instance.Get<AndroidPlugin>().U2P_CheckObtainingPermissionOverlayWindow())
            {
                UIUtil.ShowMessageBox("다른 사람들과 함께 오픈플래닛을\n사용하려면 다른 앱 위에 표시\n권한을 허용해야 합니다.", "", "취소", () => { }, "이동", () => {
                    GHPermission.RequestPermissionOveray();
                });
                return;
            }
            //

            if (OpenChatRoomTitleInput.value.IsEmpty())
            {
                UIUtil.ShowMessageBox("플래닛 제목을 입력하지 않으셨습니다. 입력해 주세요.");
                return;
            }

            // [gh042204] 여기서 #및 띄어쓰기 확인.
            string[] list = OpenChatRoomTagInput.value.Replace('#', ' ').Split(' ');
            string tag = "";
            string _str = "";
            for(int i=0;i<list.Length;i++)
            {
                _str = list[i].Trim();
                if (_str.Length > 0)
                {
                    tag += "#" + _str;
                }
            }
            OpenChatRoomTagInput.value = tag;
            // end

            UIUtil.OpenStopWnd(true);

            // [gh042406] 팝업으로 위치 이동.
            UIUtil.GetWnd<OpenChatWnd>().SetWaitRoomJoin(true, GameObject.Find("PopupWnd").transform);

            // [gh061101] 로그 기록.
            PanalyzerUtil.PAN_menuItem("DE", "S", tag);
            PanalyzerUtil.PAN_menuItem("DG", "X", MaxPeopleNumber.ToString());

            StartCoroutine(OnSend_MakeOpenChatRoom(
                       OpenChatRoomTitleInput.value,
                       OpenChatRoomTagInput.value,
                       MaxPeopleNumber,
                       (bool bSuccess) =>
                       {
                           if (bSuccess)
                           {
                               StartCoroutine(OnSend_EnterOpenChatRoom(RoomManager.GetCurrThreadKey(), (bool success, string handleId, string secret) =>
                               {
                                   if (success)
                                   {
                                       VideoCallManager.instance.StartOpenChatVideo(eCallType.VideoCall, handleId, RoomManager.GetCurrThreadKey(), secret);
                                       // [gh041706] 서버에서 늦게 처리되어도 무조건 프로그래스 바를 중단 시킨다.
                                       UIUtil.CloseStopWnd();
                                   }
                                   else
                                   {
                                       UIUtil.GetWnd<OpenChatWnd>().SetWaitRoomJoin(false);
                                   }
                               }));
                           }
                           else
                           {
                               UIUtil.GetWnd<OpenChatWnd>().SetWaitRoomJoin(false);
                           }

                           UIUtil.CloseStopWnd();
                       }));
        }

        /// <summary>
        /// OpenChatRoom 개설.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="hashTag"></param>
        /// <param name="maxCount"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_MakeOpenChatRoom(string roomName, string hashTag, int maxCount, Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_MakeOpenchatRoom_REQ(roomName, hashTag, maxCount, (bool bSuccess) =>
            {
                Success = bSuccess;

                if (Success)
                {
                }
            }));

            onComplete(Success);
        }

        /// <summary>
        /// 오픈플래닛방 입장.
        /// </summary>
        /// <param name="threadkey"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_EnterOpenChatRoom(string threadkey, Action<bool, string, string> onComplete)
        {
            bool Success = false;
            string HandleID = string.Empty;
            string Secret = string.Empty;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_OpenChatRoom_Visit(threadkey, (bool bSuccess, string handleId, string secret) =>
            {
                Success = bSuccess;

                if (Success)
                {
                    HandleID = handleId;
                    Secret = secret;
                }
            }));

            onComplete(Success, HandleID, Secret);
        }

        /// <summary>
        /// 인원수 설정 옵션.
        /// </summary>
        /// <param name="val"></param>
        public void OnClick_MaxPeopleNum(UIPopupList val)
        {
            if (val.selectIndex == 0)        //  인원수 편집
            {
                UItext[2].text = "2";
                MaxPeopleNumber = 2;
            }
            else if (val.selectIndex == 1)
            {
                UItext[2].text = "3";
                MaxPeopleNumber = 3;
            }
            else if (val.selectIndex == 2)
            {
                UItext[2].text = "4";
                MaxPeopleNumber = 4;
            }
            else if (val.selectIndex == 3)
            {
                UItext[2].text = "5";
                MaxPeopleNumber = 5;
            }
            else if (val.selectIndex == 4)
            {
                UItext[2].text = "6";
                MaxPeopleNumber = 6;
            }
            else if (val.selectIndex == 5)
            {
                UItext[2].text = "7";
                MaxPeopleNumber = 7;
            }
            else if (val.selectIndex == 6)
            {
                UItext[2].text = "8";
                MaxPeopleNumber = 8;
            }
        }
    }
}
