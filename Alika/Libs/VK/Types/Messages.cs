using Alika.Libs.VK.Responses;
using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace Alika.Libs.VK
{
    public partial class VK
    {
        public class Messages
        {
            private VK vk;

            public Messages(VK vk)
            {
                this.vk = vk;
            }

            /// <summary>
            /// messages.send
            /// </summary>
            public int Send(int peer_id, string text = null, string payload = null, List<string> attachments = null, int? sticker_id = null)
            {
                Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
                request.Add("random_id", 0);
                request.Add("peer_id", peer_id);
                if (sticker_id != null) request.Add("sticker_id", sticker_id);
                if (text != null) request.Add("message", text);
                if (payload != null) request.Add("payload", payload);
                if (attachments != null) request.Add("attachment", String.Join(",", attachments));
                return this.vk.Call<int>("messages.send", request);
            }

            /// <summary>
            /// messages.getConversations
            /// </summary>
            public GetConversationsResponse GetConversations(int offset = 0, int count = 20, string filter = "all", int start_message_id = 0, string fields = "")
            {
                Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
                request.Add("offset", offset);
                request.Add("count", count);
                request.Add("filter", filter);
                if (start_message_id > 0) request.Add("start_message_id", start_message_id);
                if (fields.Length > 0)
                {
                    request.Add("fields", fields);
                    request.Add("extended", 1);
                }
                GetConversationsResponse response = this.vk.Call<GetConversationsResponse>("messages.getConversations", request);
                App.cache.Update(response.conversations);
                App.cache.Update(response.profiles);
                App.cache.Update(response.groups);
                return response;
            }

            /// <summary>
            /// messages.getConversationsById
            /// </summary>
            public GetConversationsResponse GetConversationsById(List<int> peer_ids, string fields = "")
            {
                Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
                if (fields.Length > 0)
                {
                    request.Add("fields", fields);
                    request.Add("extended", 1);
                }
                request.Add("peer_ids", String.Join(",", peer_ids));
                GetConversationsResponse response = this.vk.Call<GetConversationsResponse>("messages.getConversationsById", request);
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
                Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
                request.Add("peer_id", peer_id);
                request.Add("offset", offset);
                request.Add("count", count);
                if (start_message_id > 0) request.Add("start_message_id", start_message_id);
                if (rev) request.Add("rev", 1);
                if (fields.Length > 0)
                {
                    request.Add("extended", 1);
                    request.Add("fields", fields);
                }
                GetHistoryResponse response = this.vk.Call<GetHistoryResponse>("messages.getHistory", request);
                App.cache.Update(response.profiles);
                App.cache.Update(response.groups);
                return response;
            }
            
            /// <summary>
            /// messages.getByid
            /// </summary>
            public GetHistoryResponse GetById(List<int> msg_ids, string fields = "")
            {
                Dictionary<string, dynamic> request = new Dictionary<string, dynamic>();
                if (fields.Length > 0)
                {
                    request.Add("fields", fields);
                    request.Add("extended", 1);
                }
                request.Add("message_ids", String.Join(",", msg_ids));
                GetHistoryResponse response = this.vk.Call<GetHistoryResponse>("messages.getById", request);
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
                RestClient http = new RestClient(this.vk.Call<UploadServers.PhotoMessages>("photos.getMessagesUploadServer", new Dictionary<string, dynamic> {
                    {"peer_id", peer_id}
                }).upload_url);
                var upload = http.Post(request);
                var response = JsonConvert.DeserializeObject<UploadServers.PhotoMessages.UploadResult>(upload.Content);
                return this.vk.Call<List<Attachment.Photo>>("photos.saveMessagesPhoto", new Dictionary<string, dynamic> {
                    {"hash", response.hash},
                    {"photo", response.photo},
                    {"server", response.server }
                })[0];
            }

            /// <summary>
            /// Uploading document from bytes (for uploading from Clipboard)
            /// </summary>
            public object UploadDocument(byte[] bytes, int peer_id, string type = "doc")
            {
                RestRequest request = new RestRequest();
                request.AddFile("file", bytes, "file.da");
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
                RestClient http = new RestClient(this.vk.Call<UploadServers.DocumentMessages>("docs.getMessagesUploadServer", new Dictionary<string, dynamic> {
                    {"peer_id", peer_id},
                    {"type", type}
                }).upload_url);
                var upload = http.Post(request);
                var response = JsonConvert.DeserializeObject<UploadServers.DocumentMessages.UploadResult>(upload.Content);
                var result = this.vk.Call<UploadServers.DocumentMessages.SaveResult>("docs.save", new Dictionary<string, dynamic> {
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
        }
    }
}
