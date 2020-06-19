using Assets.Scripts.Networks.Web;
using Assets.Scripts.UI.Windows.Popup;
using Assets.Scripts.User;
using Assets.Scripts.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.UI.Windows.Lobby.OpenChat
{
    public class OpenChat_CallUserListItem : HM_ScrollObject
    {
        public UIToggle uiPhotoToggle = null;
        public UITexture uiPhoto = null;
        // [gh042211] 랜덤하게 표시 위해.
        public UITexture nonePhoto = null;
        public UIButton uiProfile = null;

        public UILabel uiNickname = null;
        public GameObject btn_Exit_obj = null;
        public GameObject uiOwner = null;
        public GameObject me = null;

        private long usn = 0;
        private bool openProfile = false;

        // [gh042201] 정적으로 변경.
        [HideInInspector]
        public bool btn_Exit_Active;

        // [gh050307] 룸 정보 소유.
        private RoomMemberData rm;

        public override void Draw(object data, poolingInfo drawData)
        {
            var list = GetReference<List<RoomMemberData>>(data);

            try
            {
                rm = null;
                rm = list[drawData.dataIndex];
                uiNickname.text = rm.data.OpenChatNickName;
                usn = rm.userNumber;

                // [gh050309] 프로필 이미지 수정. [gh050803] 다시 기본 이미지는 나오도록.
                if (rm.data.isOpenChatRoomProfileAllow)
                {
                    //uiPhoto.mainTexture = rm.data.profileTexture;
                    uiPhoto.material = UIUtil.SetProfilePhotoMaterial(rm.data.profileTexture);
                    uiPhotoToggle.value = true;
                }
                else
                {
                    uiPhoto.material = null;
                    uiPhoto.mainTexture = null;
                    // [gh042211] 아이피 캐릭터로.
                    int rand = UnityEngine.Random.Range(0, GHGlobal.H.ipCharTexRect.Length);
                    // [gh042509] 동그란 이미지로.
                    // nonePhoto.mainTexture = GHGlobal.H.ipCharTexRect[rand];
                    nonePhoto.material = UIUtil.SetProfilePhotoMaterial(GHGlobal.H.ipCharTexRect[rand]);
                    uiPhotoToggle.value = false;
                }

                openProfile = rm.data.isOpenChatRoomProfileAllow;

                if (rm.data.isOpenChatOwner)
                {
                    uiOwner.SetActive(true);
                }
                else
                {
                    uiOwner.SetActive(false);
                }

                /*
                if (MyInfo.instance.UserData.isOpenChatOwner)
                {
                    btn_Exit_obj.SetActive(true);
                }
                else
                {
                    btn_Exit_obj.SetActive(false);
                }
                */

                if (usn == MyInfo.instance.UserData.xid)
                {
                    me.SetActive(true);
                }
                else
                    me.SetActive(false);

                // [gh042110] 강퇴 버튼 되살리기.
                // [gh042201] 방장인 경우만 강퇴 가능하게 변경.
                if (MyInfo.instance.UserData.isOpenChatOwner)
                {
                if (!me.activeSelf && !uiOwner.activeSelf)
                {
                    btn_Exit_obj.SetActive(true);
                }
                else
                    btn_Exit_obj.SetActive(false);
                }
                else
                    btn_Exit_obj.SetActive(false);
            }
            catch
            {
                Debug.LogError("GH:오픈 채팅방 멤버 정보 표시하는데 실패했습니다.");
            }
        }

        public void MyInfoInit(RoomMemberData myData)
        {
            if (myData.data.profileTexture != null)
            {
                uiPhoto.material = UIUtil.SetProfilePhotoMaterial(myData.data.profileTexture);
                uiPhotoToggle.value = true;
            }
            else
            {
                uiPhoto.material = null;
                uiPhoto.mainTexture = null;
                uiPhotoToggle.value = false;
            }

            if (myData.data.isOpenChatOwner)
            {
                uiOwner.SetActive(true);
            }
            else
            {
                uiOwner.SetActive(false);
            }

            me.SetActive(true);
        }

        public void OnClick_MemberKickButton()
        {
            UIUtil.ShowMessageBox(uiNickname.text + "님을 플래닛에서 내보내시겠습니까?", "", "취소", () => { UIUtil.SwipeBack(); }, "확인", () =>
            {
                UIUtil.OpenStopWnd(true);
                StartCoroutine(OnSend_OpenChatRoomKick(usn, (bool bSuccess) =>
                {
                    if (bSuccess)
                    {
                        Debug.LogError("추방");
                    }
                    else
                    {
                        Debug.LogError("추방실패");
                    }

                    UIUtil.CloseStopWnd();
                }));
            });
        }

        /// <summary>
        /// 오픈 플래닛 프로필 클릭.
        /// </summary>
        /// 
        // [gh042313] 프로필 중복 방지.
        private bool isOpenProfile = false;
        public void OnClick_Profile()
        {
            if (isOpenProfile)
                return;
            long my_seq = MyInfo.instance.UserData.xid;
            isOpenProfile = true;
            if (my_seq == usn)
            {
                StartCoroutine(WebProtocols.instance.GetUserProfile((string)MyInfo.instance.accessSession, (int)ResizingType.Large, (bool bSuccess, UserData uData) =>
                {
                    if (!bSuccess)
                    {
                        UIUtil.GetWnd<POP_MessageBox>().Open("", "유저 정보 조회에 실패하였습니다.");
                        return;
                    }
                    MyInfo.instance.UserData = uData;
                    UIUtil.OpenWnd(eWindow.POP_UserProfile, eUserProfileType.MyInfo, MyInfo.instance.UserData);
                    isOpenProfile = false;
                }));
            }
            else
            {
                StartCoroutine(OnSend_OpenChatFriendProfile(usn, (bool Success) =>
                {
                    isOpenProfile = false;
                }));
            }
        }

        public IEnumerator OnSend_OpenChatFriendProfile(long usn, Action<bool> onComplete)
        {
            // [gh050307] 유저 정보 검색후 처리로 변경.
            if(rm == null)
            {
                onComplete(false);
                yield break;
            }

            UserData data = rm.data;
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OpenChatGetOtherUserProfile((string)MyInfo.instance.accessSession,
                        MyInfo.instance.UserData.xid, usn, true, (int)ResizingType.Large,
                    (bool bSuccess, Recv_UserProfile uData) =>
                    {
                        Success = bSuccess;
                        if (!bSuccess)
                        {
                            UIUtil.GetWnd<POP_MessageBox>().Open("", "유저 정보 조회에 실패하였습니다.");
                            return;
                        }
                        data.line_memo = uData.lineMemo;
                        data.view_Photo_index = uData.file_Profile_idx;
                        data.view_avatar_index = uData.file_Avatar_idx;
                        data.nicknameAlias = uData.nickName;
                        data.userprofileLevel = uData.userprofileLevel;
                        data.profile_state = uData.profile_state;
                        data.xid = usn;
                        data.gender = uData.gender;
                        data.avatars = uData.avatainfo;
                        data.delegateAvatarID = uData.currentCharacterID;
                        data.currentCharacterID = uData.currentCharacterID;
                        data.profileTexture = uData.profileTexture;

                        if (uData.recv_userinterstinfo.Count > 0)
                        {
                            data.userInterestInfo.Clear();

                            for (int i = 0; i < uData.recv_userinterstinfo.Count; i++)
                            {
                                UserInterestInfo recv_data = new UserInterestInfo();

                                recv_data.Interest_index = uData.recv_userinterstinfo[i].Interest_index;
                                recv_data.uiInterest_memo = uData.recv_userinterstinfo[i].uiInterest_memo;
                                recv_data.view_Interest_index = uData.recv_userinterstinfo[i].view_Interest_index;

                                data.userInterestInfo.Add(recv_data);
                            }
                        }
                        data.isPermitAdd = uData.isPermitAdd;
                        data.eyeDist = uData.eyeDist;
                        data.eyeCorrectYValue = uData.eyeCorrectYValue;
                        data.eyeCorrectDepthValue = uData.eyeCorrectDepthValue;
                    }));

            yield return StartCoroutine(OnSend_OpenChatDownloadFriendInterestProfile(data, (bool bSuccess) =>
            {
                if (!bSuccess)
                {
                    Debug.LogError("관심사 이미지 다운로드 실패");
                }
            }));

            UIUtil.OpenWnd(eWindow.POP_UserProfile, eUserProfileType.OpenChatUser, data);

            onComplete(Success);
        }

        public IEnumerator OnSend_OpenChatDownloadFriendInterestProfile(UserData data, Action<bool> onComplete)
        {
            bool Success = false;

            // [gh042605] 관심사 0인 경우 처리.
            if (data.userprofileLevel != 2 || data.userInterestInfo.Count == 0)
            {
                onComplete(true);
                yield break;
            }

            if (data.userInterestInfo.Count > 0 && data.userprofileLevel != 0)
            {
                for (int i = 0; i < data.userInterestInfo.Count; i++)
                {
                    yield return StartCoroutine(WebProtocols.instance.DownLoad_Photo(data.userInterestInfo[i].view_Interest_index, (string)MyInfo.instance.accessSession, MyInfo.instance.deviceID, (int)UI.ResizingType.Small, (int)eFileType.ATTATION_IMAGE, (bool bSuccess, Texture2D photo) =>
                    {
                        Success = bSuccess;
                        if (bSuccess)
                        {
                            data.userInterestInfo[i].uiInterest_texture = photo;
                        }
                        else
                        {
                            data.userInterestInfo[i].uiInterest_texture = null;
                        }
                    }));
                }
            }

            onComplete(Success);
        }

        public IEnumerator OnSend_OpenChatRoomKick(long usn, Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_OpenChatRoom_Kick(usn, (bool bSuccess) =>
            {
                Success = bSuccess;

                if (Success)
                {
                }
            }));

            onComplete(Success);
        }
    }
}
