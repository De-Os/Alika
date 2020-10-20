using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Alika.Libs.VK.Methods
{
    public class Messages
    {
        private readonly VK _vk;

        internal Messages(VK vk) => this._vk = vk;

        /// <summary>
        /// messages.send
        /// </summary>
        public int Send(int peer_id, string text = null, string payload = null, List<string> attachments = null, int? sticker_id = null, int reply_to = 0)
        {
            var request = new Dictionary<string, dynamic>
            {
                { "random_id", 0 },
                { "peer_id", peer_id }
            };
            if (reply_to != 0) request.Add("reply_to", reply_to);
            if (sticker_id != null) request.Add("sticker_id", sticker_id);
            if (text != null) request.Add("message", text);
            if (payload != null) request.Add("payload", payload);
            if (attachments != null) request.Add("attachment", String.Join(",", attachments));
            return this._vk.Call<int>("messages.send", request);
        }

        /// <summary>
        /// messages.getConversations
        /// </summary>
        public GetConversationsResponse GetConversations(int offset = 0, int count = 20, string filter = "all", int start_message_id = 0, string fields = "")
        {
            var request = new Dictionary<string, dynamic>
            {
                { "offset", offset },
                { "count", count },
                { "filter", filter }
            };
            if (start_message_id > 0) request.Add("start_message_id", start_message_id);
            if (fields.Length > 0)
            {
                if (!fields.Contains("online_info")) fields += ",online_info";
                request.Add("fields", fields);
                request.Add("extended", 1);
            }
            GetConversationsResponse response = this._vk.Call<GetConversationsResponse>("messages.getConversations", request);
            App.cache.Update(response.conversations);
            App.cache.Update(response.profiles);
            App.cache.Update(response.groups);
            return response;
        }

        /// <summary>
        /// messages.getConversationsById
        /// </summary>
        public GetConversationsByIdResponse GetConversationsById(List<int> peer_ids, string fields = "")
        {
            var request = new Dictionary<string, dynamic>();
            if (fields.Length > 0)
            {
                if (!fields.Contains("online_info")) fields += ",online_info";
                request.Add("fields", fields);
                request.Add("extended", 1);
            }
            request.Add("peer_ids", String.Join(",", peer_ids));
            GetConversationsByIdResponse response = this._vk.Call<GetConversationsByIdResponse>("messages.getConversationsById", request);
            App.cache.Update(response.conversations);
            App.cache.Update(response.profiles);
            App.cache.Update(response.groups);
            return response;
        }

        /// <summary>
        /// messages.getHistory
        /// </summary>
        public GetHistoryResponse GetHistory(int peer_id, int offset = 0, int count = 20, int start_message_id = 0, bool rev = false, string fields = "")
        {
            var request = new Dictionary<string, dynamic>
            {
                { "peer_id", peer_id },
                { "offset", offset },
                { "count", count }
            };
            if (start_message_id > 0) request.Add("start_message_id", start_message_id);
            if (rev) request.Add("rev", 1);
            if (fields.Length > 0)
            {
                if (!fields.Contains("online_info")) fields += ",online_info";
                request.Add("extended", 1);
                request.Add("fields", fields);
            }
            GetHistoryResponse response = this._vk.Call<GetHistoryResponse>("messages.getHistory", request);
            App.cache.Update(response.profiles);
            App.cache.Update(response.groups);
            return response;
        }

        /// <summary>
        /// messages.getByid
        /// </summary>
        public GetHistoryResponse GetById(List<int> msg_ids, string fields = "")
        {
            var request = new Dictionary<string, dynamic>();
            if (fields.Length > 0)
            {
                if (!fields.Contains("online_info")) fields += ",online_info";
                request.Add("fields", fields);
                request.Add("extended", 1);
            }
            request.Add("message_ids", String.Join(",", msg_ids));
            GetHistoryResponse response = this._vk.Call<GetHistoryResponse>("messages.getById", request);
            App.cache.Update(response.profiles);
            App.cache.Update(response.groups);
            return response;
        }

        /// <summary>
        /// messages.getConversationMembers
        /// </summary>
        public GetConversationMembersResponse GetConversationMembers(int peer_id, string fields = "")
        {
            var request = new Dictionary<string, dynamic>
            {
                { "peer_id", peer_id }
            };
            if (fields.Length > 0)
            {
                if (!fields.Contains("online_info")) fields += ",online_info";
                request.Add("fields", fields);
            }
            var response = this._vk.Call<GetConversationMembersResponse>("messages.getConversationMembers", request);
            App.cache.Update(response.profiles);
            App.cache.Update(response.groups);
            return response;
        }

        /// <summary>
        /// Uploading photo from bytes (for uploading from Clipboard)
        /// </summary>
        public Attachment.Photo UploadPhoto(byte[] bytes, int peer_id)
        {
            RestRequest request = new RestRequest();
            request.AddFile("photo", bytes, "image.jpg");
            return this.UploadPhoto(request, peer_id);
        }

        /// <summary>
        /// Uploading StorageFile photo
        /// </summary>
        public async Task<Attachment.Photo> UploadPhoto(StorageFile file, int peer_id)
        {
            RestRequest request = new RestRequest();
            request.AddFile("photo", await file.ReadBytesAsync(), file.Name, file.ContentType);
            return this.UploadPhoto(request, peer_id);
        }

        private Attachment.Photo UploadPhoto(RestRequest request, int peer_id)
        {
            RestClient http = new RestClient(this._vk.Call<UploadServers.PhotoMessages>("photos.getMessagesUploadServer", new Dictionary<string, dynamic> {
                    {"peer_id", peer_id}
                }).upload_url);
            var upload = http.Post(request);
            var response = JsonConvert.DeserializeObject<UploadServers.PhotoMessages.UploadResult>(upload.Content);
            return this._vk.Call<List<Attachment.Photo>>("photos.saveMessagesPhoto", new Dictionary<string, dynamic> {
                    {"hash", response.hash},
                    {"photo", response.photo},
                    {"server", response.server }
                })[0];
        }

        /// <summary>
        /// Uploading document from bytes (for uploading from Clipboard/graffiti)
        /// </summary>
        public object UploadDocument(byte[] bytes, int peer_id, string type = "doc")
        {
            RestRequest request = new RestRequest();
            switch (type)
            {
                case "doc":
                    request.AddFile("file", bytes, "file.da");
                    break;
                case "graffiti":
                    request.AddFile("file", bytes, "file.png");
                    break;
            }
            return this.UploadDocument(request, type, peer_id);
        }
        /// <summary>
        /// Uploading StorageFile document
        /// </summary>
        public async Task<object> UploadDocument(StorageFile file, int peer_id, string type = "doc")
        {
            RestRequest request = new RestRequest();
            request.AddFile("file", await file.ReadBytesAsync(), file.Name, file.ContentType);
            return this.UploadDocument(request, type, peer_id);
        }

        private object UploadDocument(RestRequest request, string type, int peer_id)
        {
            RestClient http = new RestClient(this._vk.Call<UploadServers.DocumentMessages>("docs.getMessagesUploadServer", new Dictionary<string, dynamic> {
                    {"peer_id", peer_id},
                    {"type", type}
                }).upload_url);
            var upload = http.Post(request);
            var response = JsonConvert.DeserializeObject<UploadServers.DocumentMessages.UploadResult>(upload.Content);
            var result = this._vk.Call<UploadServers.DocumentMessages.SaveResult>("docs.save", new Dictionary<string, dynamic> {
                    {"file", response.file}
                });
            if (type == "graffiti")
            {
                return result.graffiti;
            }
            else if (type == "audio_message")
            {
                return result.audio_message;
            }
            else
            {
                return result.document;
            }
        }

        /// <summary>
        /// messages.getHistoryAttachments
        /// </summary>
        public GetHistoryAttachmentsResponse GetHistoryAttachments(int peer_id, string type, string start_from = null, int count = 50)
        {
            var request = new Dictionary<string, dynamic>
            {
                { "peer_id", peer_id },
                { "media_type", type },
                { "count", count }
            };
            if (start_from != null) request.Add("start_from", start_from);
            return this._vk.Call<GetHistoryAttachmentsResponse>("messages.getHistoryAttachments", request);
        }

        /// <summary>
        /// messages.removeChatUser
        /// </summary>
        public int RemoveChatUser(int peer_id, int member_id)
        {
            if (peer_id > Limits.Messages.PEERSTART) peer_id -= Limits.Messages.PEERSTART;
            return this._vk.Call<int>("messages.removeChatUser", new Dictionary<string, dynamic>
            {
                { "chat_id", peer_id },
                { "member_id", member_id }
            });
        }

        /// <summary>
        /// messages.setMemberRole
        /// </summary>
        public int SetMemberRole(bool admin, int member_id, int peer_id)
        {
            return this._vk.Call<int>("messages.setMemberRole", new Dictionary<string, dynamic>
            {
                { "peer_id", peer_id },
                { "member_id", member_id },
                {"role", admin ? "admin" : "member" }
            });
        }

        /// <summary>
        /// messages.deleteChatPhoto
        /// </summary>
        public ChangeChatPhotoResponse DeleteChatPhoto(int peer_id)
        {
            if (peer_id > Limits.Messages.PEERSTART) peer_id -= Limits.Messages.PEERSTART;
            return this._vk.Call<ChangeChatPhotoResponse>("messages.deleteChatPhoto", new Dictionary<string, dynamic> {
                {"chat_id", peer_id}
            });
        }

        /// <summary>
        /// messages.editChat
        /// </summary>
        public int EditTitle(int peer_id, string title)
        {
            if (peer_id > Limits.Messages.PEERSTART) peer_id -= Limits.Messages.PEERSTART;
            return this._vk.Call<int>("messages.editChat", new Dictionary<string, dynamic> {
                {"chat_id", peer_id},
                {"title", title}
            });
        }

        /// <summary>
        /// photos.getChatUploadServer
        /// </summary>
        public ChangeChatPhotoResponse SetChatPhoto(int peer_id, byte[] bytes)
        {
            RestRequest request = new RestRequest();
            request.AddFile("photo", bytes, "file.png");
            return this.SetChatPhoto(request, peer_id);
        }

        /// <summary>
        /// photos.getChatUploadServer
        /// </summary>
        public async Task<ChangeChatPhotoResponse> SetChatPhoto(StorageFile file, int peer_id)
        {
            RestRequest request = new RestRequest();
            request.AddFile("photo", await file.ReadBytesAsync(), file.Name, file.ContentType);
            return this.SetChatPhoto(request, peer_id);
        }

        private ChangeChatPhotoResponse SetChatPhoto(RestRequest request, int peer_id)
        {
            if (peer_id > Limits.Messages.PEERSTART) peer_id -= Limits.Messages.PEERSTART;
            RestClient http = new RestClient(this._vk.Call<UploadServers.ChatPhoto>("photos.getChatUploadServer", new Dictionary<string, dynamic> {
                    {"chat_id", peer_id}
                }).upload_url);
            var upload = http.Post(request);
            var response = JsonConvert.DeserializeObject<BasicResponse<string>>(upload.Content);
            return this._vk.Call<ChangeChatPhotoResponse>("messages.setChatPhoto", new Dictionary<string, dynamic> {
                    {"file", response.response}
                });
        }

        /// <summary>
        /// messages.searchConversations
        /// </summary>
        public GetConversationsByIdResponse SearchConversations(string query, int count = 20, string fields = "")
        {
            var request = new Dictionary<string, dynamic>
            {
                { "q", query },
                { "count", count },
            };
            if (fields.Length > 0)
            {
                if (!fields.Contains("online_info")) fields += ",online_info";
                request.Add("fields", fields);
                request.Add("extended", 1);
            }
            GetConversationsByIdResponse response = this._vk.Call<GetConversationsByIdResponse>("messages.searchConversations", request);
            App.cache.Update(response.conversations);
            App.cache.Update(response.profiles);
            App.cache.Update(response.groups);
            return response;
        }

        /// <summary>
        /// messages.markAsRead
        /// </summary>
        public int MarkAsRead(int peer_id, List<int> messages)
        {
            return this._vk.Call<int>("messages.markAsRead", new Dictionary<string, dynamic> {
                {"peer_id", peer_id},
                {"message_ids", String.Join(",", messages)}
            });
        }
    }
}
