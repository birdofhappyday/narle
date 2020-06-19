using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using Assets.Scripts.Utils;
using Assets.Scripts.UI.Windows.Lobby.VideoCallWindow;
using Assets.Scripts.Networks.Web;
using Assets.Scripts.Messenger;
using Assets.Scripts.User;
using System.Collections;
using System.Linq;
using System;
using Assets.Scripts.Cores;

namespace Assets.Scripts.UI.Windows.Lobby.OpenChat
{
    public class OpenChat_ChatroomListItem : HM_ScrollObject
    {
        [System.Serializable]
        public class MemberStateObject : ControlHelper
        {
            //[HideInInspector] public videoPlayerController profile;
            [HideInInspector] public UITexture profile;
            [HideInInspector] public UISprite nonProfile;
            [HideInInspector] public UITexture limitProfile;    // [gh041904] uitexture로 변경.

            public override void Init()
            {
                try
                {
                    //[gh042312] 오픈플래닛 변경.
                    //profile = GetControl<videoPlayerController>(mainGO, "Profile");
                    profile = GetControl<UITexture>(mainGO, "Profileimage");
                    profile.mainTexture = null;
                    nonProfile = GetControl<UISprite>(mainGO, "NonProfile");
                    limitProfile = GetControl<UITexture>(mainGO, "LimitProfile");
                    limitProfile.mainTexture = null;
                }
                catch
                {
                    Debug.LogError("OpenChat_ChatroomListItem의 Init 실패");
                }
            }
        }

        //public VideoClip[] videoClips;
        public MemberStateObject[] MemberProfile;
        public UILabel UIOpenChatRoomName;
        public UILabel UIOpenChatHashTag;
        public UILabel UIOpenChatMemNumber; // [gh041003] 멤버 수.
        public Shader Shader;

        private int MaxMemberNum;
        private bool init = false;
        private Texture2D OwnerTex;

        private string threadKey = string.Empty;
        //static int random = 1;

        // [gh041004] 멤버 표시가 바뀌어서 처리한다.
        public Transform hideRoot;
        public UIScrollView memberScroll;
        public UIGrid memberGrid;

        // [gh041901] 랜덤 프로필 이미지. -> 삭제
        // public Texture2D[] randomLimitImage;

        protected override void Awake()
        {
            for (int i = 0; i < MemberProfile.Length; i++)
            {
                MemberProfile[i].Init();
            }

            List<Texture2D> ImageList = new List<Texture2D>();
            // [gh041103] 드래그 이벤트 추가.
            memberScroll.onDragStarted += OnMemDragStart;
            memberScroll.onDragFinished += OnMemDragEnd;
        }

        private void OnMemDragStart()
        {
            UIManager.isSwipeStop = true;
        }

        private void OnMemDragEnd()
        {
            UIManager.isSwipeStop = false;
        }

        private void Update()
        {

        }

        public override void Draw(object data, poolingInfo drawData)
        {
            var list = GetReference<List<RoomInfo>>(data);

            try
            {
                RoomInfo ri = list[drawData.dataIndex];
                threadKey = ri.threadKey;
                // Chatroom Name.
                UIOpenChatRoomName.text = ri.OpenChatRoomName;
                // [gh041605] 글자 수 15보다 많으면 ...
                if (UIOpenChatRoomName.text.Length > 15)
                {
                    UIOpenChatRoomName.text = UIOpenChatRoomName.text.Substring(0, 15) + "...";
                }
                // hashTag.
                UIOpenChatHashTag.text = ri.OpenchatHashTag;
                // [gh041605] 글자 수 15보다 많으면 ...
                if (UIOpenChatHashTag.text.Length > 15)
                {
                    UIOpenChatHashTag.text = UIOpenChatHashTag.text.Substring(0, 15) + "...";
                }

                // 방 인원 수.
                MaxMemberNum = ri.OpenChatMaxCount;
                // [gh041004] 인원수 표기.
                UIOpenChatMemNumber.text = ri.OpenChatCurrentCount + "/" + MaxMemberNum;
                MemberImageSetting();

                var roomMemberDatas = ri.openChatMemberList.Values.ToList();
                List<Texture2D> texList = new List<Texture2D>();
                
                // [gh041007] 멤버 초기화.
                int depth = 0;
                for (int i = 1; i < MemberProfile.Length; i++)
                {
                    if (i < MaxMemberNum)
                    {
                        MemberProfile[i].mainGO.transform.parent = memberGrid.transform;
                    }
                    else
                    {
                        MemberProfile[i].mainGO.transform.parent = hideRoot;
                    }
                    // [gh050811] bg추가로 인한 처리.
                    depth = (20 - i * 2);
                    MemberProfile[i].limitProfile.depth = depth;
                    MemberProfile[i].nonProfile.depth = depth;
                    MemberProfile[i].profile.depth = depth;
                    MemberProfile[i].mainGO.transform.Find("bg").GetComponent<UISprite>().depth = depth-1;
                }

                memberGrid.repositionNow = false;
                memberScroll.contentPivot = UIWidget.Pivot.TopLeft;
                // [gh050810] 스크롤 삭제 및 6인방 처리
                float startX = -memberScroll.panel.width / 2;
                if (MaxMemberNum <= 6)    // 6명 이하는 기존과 동일.
                {
                    startX -= 148;
                    for (int i = 1; i < MemberProfile.Length; i++)
                    {
                        MemberProfile[i].mainGO.transform.localPosition = new Vector3(startX + i * 148, 0);
                    }
                }
                else
                {
                    float width = (memberScroll.panel.width - 74) / (MaxMemberNum-1);
                    // [gh050901] 108로 값 수정.
                    float margin = (width - 108) / (MaxMemberNum-1);
                    startX = startX - width + 4;
                    width = width + 4 + margin;
                    for (int i = 1; i < MemberProfile.Length; i++)
                    {
                        MemberProfile[i].mainGO.transform.localPosition = new Vector3(startX + i * width, 0);
                    }
                }
                //memberGrid.enabled = false;
                //memberScroll.ResetPosition();

                for (int i = 0; i < roomMemberDatas.Count; ++i)
                {
                    if (roomMemberDatas[i].data.isOpenChatOwner)
                    {
                        // [gh041104] 0번은 방장이다.
                        // [gh042202] 비공개 프로필도 ip캐릭터로. [gh050802] 다시 기본으로..
                        if (!roomMemberDatas[i].data.isOpenChatRoomProfileAllow)
                        {
                            int rand = UnityEngine.Random.Range(0, GHGlobal.H.ipCharTexRect.Length);
                            texList.Insert(0, GHGlobal.H.ipCharTexRect[rand]);
                        }
                        else
                        {
                            texList.Insert(0, roomMemberDatas[i].data.profileTexture);
                        }
                    }
                    else
                    {
                        texList.Add(roomMemberDatas[i].data.profileTexture);
                    }
                }

                MemberImageSort(texList);

                //int random = UnityEngine.Random.Range(1, MaxMemberNum);
                //for (int i = 0; i < random; ++i)
                //{
                //    MemberProfile[i].profile.gameObject.SetActive(false);
                //    MemberProfile[i].nonProfile.gameObject.SetActive(false);
                //    MemberProfile[i].profile.SetVideo();
                //    int randomClips = UnityEngine.Random.Range(0, videoClips.Length);
                //    MemberProfile[i].profile.GetComponentInChildren<VideoPlayer>().clip = videoClips[randomClips];
                //    MemberProfile[i].profile.GetComponentInChildren<VideoPlayer>().playOnAwake = true;
                //    MemberProfile[i].profile.gameObject.SetActive(true);
                //}

                Vector3 pos = transform.localPosition;
                pos.x = 540;
                transform.localPosition = pos;
            }
            catch
            {
                Debug.LogError("오픈플래닛방정보 표시하는데 실패했습니다.");
            }
        }

        /// <summary>
        /// 처음 방 이미지를 설정한다.
        /// 맥스 인원수를 인자로 주면 그 인원수는 기본 이미지세팅, 나머지는 X이미지를 띄운다.
        /// </summary>
        /// <param name="index"></param>
        public void MemberImageSetting()
        {
            MemberImageSetOff(true);
            /*for (int i = 0; i < MaxMemberNum; i++)
            {
                MemberSetbBasicImage(i);
            }*/

            for (int i = 0; i < 8; i++)
            {
                MemberSetLimitImage(i);
            }
        }

        /// <summary>
        /// 방 멤버 이미지 오브젝트를 끈다.
        /// </summary>
        /// <param name="index"></param>
        public void MemberImageSetOff(bool all = false, int index = 0)
        {
            if (all)
            {
                for (int i = 0; i < MemberProfile.Length; ++i)
                {
                    MemberProfile[i].profile.gameObject.SetActive(false);
                    MemberProfile[i].nonProfile.gameObject.SetActive(false);
                    MemberProfile[i].limitProfile.gameObject.SetActive(false);
                }
            }
            else
            {
                MemberProfile[index].profile.gameObject.SetActive(false);
                MemberProfile[index].nonProfile.gameObject.SetActive(false);
                MemberProfile[index].limitProfile.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 방 이미지 세팅(표시 이미지 mp4)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="image"></param>
        public void MemberSetVideo(int index, VideoClip profileVideoClip)
        {
            MemberImageSetOff(false, index);
            MemberProfile[index].nonProfile.gameObject.SetActive(false);
            MemberProfile[index].profile.gameObject.SetActive(true);
            MemberProfile[index].profile.GetComponentInChildren<VideoPlayer>().clip = profileVideoClip;
        }

        /// <summary>
        /// 방 이미지 세팅(표시 이미지 Texture)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="profileVideoClip"></param>
        public void MemberSetImage(int index, Texture2D profileImage)
        {
            MemberImageSetOff(false, index);
            MemberProfile[index].nonProfile.gameObject.SetActive(false);
            MemberProfile[index].profile.gameObject.SetActive(true);
            MemberProfile[index].profile.material = UIUtil.SetProfilePhotoMaterial(profileImage);
            //MemberProfile[index].profile.GetComponentInChildren<UITexture>().mainTexture = profileImage;
        }

        /// <summary>
        /// 방 이미지 세팅(들어올 수 있고 사람이 없다는 이미지)
        /// 사람이 나갔을 때도 사용.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="image"></param>
        private void MemberSetbBasicImage(int index)
        {
            if (index >= MemberProfile.Length)
                return;
            MemberImageSetOff(false, index);
            MemberProfile[index].nonProfile.gameObject.SetActive(true);
        }

        /// <summary>
        /// 서버에서 동영상 파일을 받아와서 세팅한다.
        /// 동영상 형식에 따라 매개변수를 변경하고 프리팹 설정을 바꾼다.
        /// </summary>
        /// <param name="memberImageList"></param>
        private void MemberVideoSort(List<VideoClip> memberImageList)
        {
            for (int i = 0; i < memberImageList.Count; ++i)
            {
                MemberProfile[i].profile.GetComponentInChildren<VideoPlayer>().clip = memberImageList[i];
            }
        }


        /// <summary>
        /// 서버에서 프로필 이미지를 받아와서 세팅한다.
        /// </summary>
        /// <param name="memberImageList"></param>
        private void MemberImageSort(List<Texture2D> memberImageList)
        {
            for (int i = 0; i < memberImageList.Count; ++i)
            {
                MemberSetImage(i, memberImageList[i]);
            }
        }

        /// <summary>
        /// 방의 이미지 세팅(들어 올 수 없다는 이미지)
        /// </summary>
        /// <param name="index"></param>
        /// <param name="image"></param>
        private void MemberSetLimitImage(int index)
        {
            MemberImageSetOff(false, index);
            // [gh041902] 랜덤 처리.
            int rand = UnityEngine.Random.Range(0, GHGlobal.H.ipCharTexPlus.Length);
            MemberProfile[index].limitProfile.material = UIUtil.SetProfilePhotoMaterial(GHGlobal.H.ipCharTexPlus[rand], 0);
            //

            MemberProfile[index].limitProfile.gameObject.SetActive(true);
        }

        /// <summary>
        /// 방 참가 버튼 클릭.
        /// </summary>
        public void OnClick_OpenChatRoomVisiteButton()
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

            // [gh061101] 로그 기록.
            PanalyzerUtil.PAN_menuItem("DD", "C", "");

            UIUtil.OpenStopWnd(true);

            var openChatWnd = UIUtil.GetWnd<OpenChatWnd>();
            // [gh042406] 팝업으로 위치 이동.
            openChatWnd.SetWaitRoomJoin(true, GameObject.Find("PopupWnd").transform);
            WebProcessor.instance.StartCoroutine(openChatWnd.OnSend_EnterOpenChatRoom(
                       threadKey,
                       (bool bSuccess, string handleid, string secret) =>
                       {
                           if (bSuccess)
                           {
                               RoomManager.SetThreadKey(threadKey, ROOMTYPE.OPEN);
                               VideoCallManager.instance.StartOpenChatVideo(eCallType.VideoCall, handleid, RoomManager.GetCurrThreadKey(), secret);
                           }
                           else
                           {
                               OnClick_OpenChatRoomExit();
                           }

                           UIUtil.CloseStopWnd();
                       }));
        }

        public void OnClick_OpenChatRoomExit()
        {
            UIUtil.OpenStopWnd(true);

            var openChatWnd = UIUtil.GetWnd<OpenChatWnd>();
            StartCoroutine(openChatWnd.OnSend_OpenChatRoomExit(threadKey,
                       (bool bSuccess) =>
                       {
                           openChatWnd.RefreshWnd(true);
                           UIUtil.CloseStopWnd();
                           openChatWnd.SetWaitRoomJoin(false);  // [gh042901] 무조건 처리용.
                           //UIUtil.GetWnd<VideoCallWnd>().OpenChatCallingEnd();
                           //UIUtil.Swipe(eWindow.OpenChatWnd);

                       }));
        }
    }
}
