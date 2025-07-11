using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DoanKhoaClient.Services
{
    public class CommentService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;

        public CommentService(string baseUrl = "http://localhost:5299")
        {
            _baseUrl = baseUrl?.TrimEnd('/') ?? "http://localhost:5299";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        #region Public Methods

        /// <summary>
        /// Lấy danh sách bình luận theo ID hoạt động
        /// </summary>
        public async Task<List<Comment>> GetCommentsByActivityIdAsync(string activityId, CancellationToken cancellationToken = default, string userId = null)
        {
            try
            {
                ValidateActivityId(activityId);

                var url = $"/api/Comment/activity/{activityId}";
                if (!string.IsNullOrEmpty(userId))
                {
                    url += $"?userId={Uri.EscapeDataString(userId)}";
                }

                LogRequest($"Getting comments for activity: {activityId}");

                var response = await _httpClient.GetAsync(url, cancellationToken);
                await EnsureSuccessResponse(response);

                var json = await response.Content.ReadAsStringAsync();
                LogResponse($"Comments response: {json}");

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<Comment>();
                }

                var commentResponses = JsonSerializer.Deserialize<List<CommentResponse>>(json, _jsonOptions);
                var comments = commentResponses?.Select(MapToComment)
                                              .Where(c => c != null)
                                              .ToList() ?? new List<Comment>();

                // Populate reply information cho tất cả comments
                PopulateReplyInformation(comments);

                // Sắp xếp comments: comments gốc trước, theo thời gian tăng dần
                // Replies sẽ được hiển thị ngay sau comment gốc tương ứng
                var sortedComments = OrganizeCommentsWithReplies(comments);

                LogInfo($"Successfully loaded and organized {sortedComments.Count} comments");
                return sortedComments;
            }
            catch (Exception ex)
            {
                LogError($"Error getting comments for activity {activityId}: {ex.Message}");
                throw new Exception($"Không thể lấy danh sách bình luận: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Populate reply information cho tất cả comments dựa trên ParentCommentId
        /// </summary>
        private void PopulateReplyInformation(List<Comment> comments)
        {
            // Tạo dictionary để tra cứu nhanh comment theo ID
            var commentDict = comments.ToDictionary(c => c.Id, c => c);

            foreach (var comment in comments.Where(c => !string.IsNullOrEmpty(c.ParentCommentId)))
            {
                if (commentDict.TryGetValue(comment.ParentCommentId, out var parentComment))
                {
                    comment.ReplyToUserName = parentComment.UserDisplayName;
                    comment.ReplyToUserId = parentComment.UserId;
                    comment.ReplyToContent = parentComment.Content;

                    System.Diagnostics.Debug.WriteLine($"Populated reply info for comment {comment.Id}:");
                    System.Diagnostics.Debug.WriteLine($"  - ReplyToUserName: {comment.ReplyToUserName}");
                    System.Diagnostics.Debug.WriteLine($"  - ReplyToContent: {comment.ReplyToContent?.Substring(0, Math.Min(50, comment.ReplyToContent.Length))}...");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Warning: Parent comment {comment.ParentCommentId} not found for comment {comment.Id}");
                }
            }
        }

        private List<Comment> OrganizeCommentsWithReplies(List<Comment> comments)
        {
            var result = new List<Comment>();

            System.Diagnostics.Debug.WriteLine($"=== ORGANIZING {comments.Count} COMMENTS ===");

            // Tách comments gốc và replies
            var rootComments = comments.Where(c => string.IsNullOrEmpty(c.ParentCommentId))
                                      .OrderBy(c => c.CreatedAt)
                                      .ToList();

            var replies = comments.Where(c => !string.IsNullOrEmpty(c.ParentCommentId))
                                 .OrderBy(c => c.CreatedAt)
                                 .ToList();

            System.Diagnostics.Debug.WriteLine($"Root comments: {rootComments.Count}, Replies: {replies.Count}");

            // Thêm từng comment gốc và replies của nó (bao gồm replies của replies)
            foreach (var rootComment in rootComments)
            {
                result.Add(rootComment);
                System.Diagnostics.Debug.WriteLine($"Added root comment: {rootComment.Id}");

                // Thêm tất cả replies của comment này một cách đệ quy
                AddRepliesRecursively(rootComment.Id, replies, result);
            }

            System.Diagnostics.Debug.WriteLine($"Final organized list has {result.Count} comments");
            return result;
        }

        /// <summary>
        /// Thêm replies một cách đệ quy nhưng giữ cùng mức indent
        /// </summary>
        private void AddRepliesRecursively(string parentId, List<Comment> allReplies, List<Comment> result)
        {
            var directReplies = allReplies.Where(r => r.ParentCommentId == parentId)
                                         .OrderBy(r => r.CreatedAt)
                                         .ToList();

            foreach (var reply in directReplies)
            {
                result.Add(reply);
                System.Diagnostics.Debug.WriteLine($"Added reply: {reply.Id} to parent: {reply.ParentCommentId}");

                // Thêm replies của reply này (cùng mức indent)
                AddRepliesRecursively(reply.Id, allReplies, result);
            }
        }

        /// <summary>
        /// Tạo bình luận mới
        /// </summary>
        public async Task<Comment> CreateCommentAsync(CreateCommentRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateCreateCommentRequest(request);

                LogRequest($"Creating comment for activity: {request.ActivityId}");

                // Log request details
                System.Diagnostics.Debug.WriteLine($"=== CREATE COMMENT REQUEST ===");
                System.Diagnostics.Debug.WriteLine($"ActivityId: {request.ActivityId}");
                System.Diagnostics.Debug.WriteLine($"UserId: {request.UserId}");
                System.Diagnostics.Debug.WriteLine($"Content: {request.Content}");
                System.Diagnostics.Debug.WriteLine($"ParentCommentId: {request.ParentCommentId ?? "null"}");

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/Comment", content, cancellationToken);
                await EnsureSuccessResponse(response);

                var responseJson = await response.Content.ReadAsStringAsync();
                LogResponse($"Create comment response: {responseJson}");

                var commentResponse = JsonSerializer.Deserialize<CommentResponse>(responseJson, _jsonOptions);
                var comment = MapToComment(commentResponse);

                if (comment == null)
                {
                    throw new Exception("Không thể tạo bình luận - phản hồi từ server không hợp lệ");
                }

                // Đảm bảo ParentCommentId được preserve
                if (!string.IsNullOrEmpty(request.ParentCommentId))
                {
                    comment.ParentCommentId = request.ParentCommentId;
                    System.Diagnostics.Debug.WriteLine($"Ensured ParentCommentId: {comment.ParentCommentId}");
                }

                LogInfo($"Successfully created comment with ID: {comment.Id}, IsReply: {comment.IsReply}");
                return comment;
            }
            catch (Exception ex)
            {
                LogError($"Error creating comment: {ex.Message}");
                throw new Exception($"Không thể tạo bình luận: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cập nhật bình luận
        /// </summary>
        public async Task<Comment> UpdateCommentAsync(string commentId, string newContent, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateCommentId(commentId);
                ValidateContent(newContent);

                LogRequest($"Updating comment: {commentId}");

                var request = new UpdateCommentRequest { Content = newContent.Trim() };
                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"/api/Comment/{commentId}", content, cancellationToken);
                await EnsureSuccessResponse(response);

                var responseJson = await response.Content.ReadAsStringAsync();
                LogResponse($"Update comment response: {responseJson}");

                var commentResponse = JsonSerializer.Deserialize<CommentResponse>(responseJson, _jsonOptions);
                var comment = MapToComment(commentResponse);

                if (comment == null)
                {
                    throw new Exception("Không thể cập nhật bình luận - phản hồi từ server không hợp lệ");
                }

                LogInfo($"Successfully updated comment: {commentId}");
                return comment;
            }
            catch (Exception ex)
            {
                LogError($"Error updating comment {commentId}: {ex.Message}");
                throw new Exception($"Không thể cập nhật bình luận: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Xóa bình luận
        /// </summary>
        public async Task<bool> DeleteCommentAsync(string commentId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateCommentId(commentId);

                LogRequest($"Deleting comment: {commentId}");

                var response = await _httpClient.DeleteAsync($"/api/Comment/{commentId}", cancellationToken);
                var success = response.IsSuccessStatusCode;

                if (!success)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogError($"Failed to delete comment {commentId}: {response.StatusCode} - {errorContent}");
                }
                else
                {
                    LogInfo($"Successfully deleted comment: {commentId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogError($"Error deleting comment {commentId}: {ex.Message}");
                throw new Exception($"Không thể xóa bình luận: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Toggle like/unlike bình luận
        /// </summary>
        public async Task<bool> ToggleCommentLikeAsync(string commentId, CancellationToken cancellationToken = default)
        {
            try
            {
                ValidateCommentId(commentId);

                var currentUserId = GetCurrentUserId();
                if (string.IsNullOrEmpty(currentUserId))
                {
                    throw new Exception("Người dùng chưa đăng nhập");
                }

                LogRequest($"Toggling like for comment: {commentId} by user: {currentUserId}");

                var response = await _httpClient.PostAsync(
                    $"/api/Comment/{commentId}/like?userId={Uri.EscapeDataString(currentUserId)}",
                    null,
                    cancellationToken);

                var success = response.IsSuccessStatusCode;

                if (!success)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    LogError($"Failed to toggle like for comment {commentId}: {response.StatusCode} - {errorContent}");
                }
                else
                {
                    LogInfo($"Successfully toggled like for comment: {commentId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogError($"Error toggling comment like {commentId}: {ex.Message}");
                throw new Exception($"Không thể thực hiện thao tác thích bình luận: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy trạng thái bình luận của người dùng
        /// </summary>
        public async Task<Dictionary<string, bool>> GetUserCommentStatusesAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    userId = GetCurrentUserId();
                }

                if (string.IsNullOrEmpty(userId))
                {
                    return new Dictionary<string, bool>();
                }

                LogRequest($"Getting comment statuses for user: {userId}");

                var response = await _httpClient.GetAsync($"/api/Comment/user-status/{Uri.EscapeDataString(userId)}");

                if (!response.IsSuccessStatusCode)
                {
                    LogError($"Failed to get user comment statuses: {response.StatusCode}");
                    return new Dictionary<string, bool>();
                }

                var json = await response.Content.ReadAsStringAsync();
                var statuses = JsonSerializer.Deserialize<Dictionary<string, bool>>(json, _jsonOptions)
                              ?? new Dictionary<string, bool>();

                LogInfo($"Successfully loaded {statuses.Count} comment statuses");
                return statuses;
            }
            catch (Exception ex)
            {
                LogError($"Error getting user comment statuses: {ex.Message}");
                return new Dictionary<string, bool>();
            }
        }

        /// <summary>
        /// Lấy số lượng bình luận theo ID hoạt động
        /// </summary>
        public async Task<int> GetCommentCountByActivityIdAsync(string activityId)
        {
            try
            {
                var comments = await GetCommentsByActivityIdAsync(activityId);
                return comments?.Count ?? 0;
            }
            catch (Exception ex)
            {
                LogError($"Error getting comment count for activity {activityId}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Báo cáo bình luận spam (dành cho admin)
        /// </summary>
        public async Task<bool> ReportCommentAsSpamAsync(string commentId)
        {
            try
            {
                ValidateCommentId(commentId);

                LogRequest($"Reporting comment as spam: {commentId}");

                var response = await _httpClient.PostAsync($"/api/Comment/{commentId}/report-spam", null);
                var success = response.IsSuccessStatusCode;

                if (success)
                {
                    LogInfo($"Successfully reported comment as spam: {commentId}");
                }
                else
                {
                    LogError($"Failed to report comment as spam: {commentId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogError($"Error reporting comment as spam {commentId}: {ex.Message}");
                throw new Exception($"Không thể báo cáo bình luận: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Lấy danh sách bình luận spam (dành cho admin)
        /// </summary>
        public async Task<List<Comment>> GetSpamCommentsAsync()
        {
            try
            {
                LogRequest("Getting spam comments");

                var response = await _httpClient.GetAsync("/api/Comment/spam");
                await EnsureSuccessResponse(response);

                var json = await response.Content.ReadAsStringAsync();
                var commentResponses = JsonSerializer.Deserialize<List<CommentResponse>>(json, _jsonOptions);
                var comments = commentResponses?.Select(MapToComment)
                                              .Where(c => c != null)
                                              .ToList() ?? new List<Comment>();

                LogInfo($"Successfully loaded {comments.Count} spam comments");
                return comments;
            }
            catch (Exception ex)
            {
                LogError($"Error getting spam comments: {ex.Message}");
                throw new Exception($"Không thể lấy danh sách bình luận spam: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Khôi phục bình luận từ spam
        /// </summary>
        public async Task<bool> RestoreCommentFromSpamAsync(string commentId)
        {
            try
            {
                ValidateCommentId(commentId);

                LogRequest($"Restoring comment from spam: {commentId}");

                var response = await _httpClient.PostAsync($"/api/Comment/{commentId}/restore", null);
                var success = response.IsSuccessStatusCode;

                if (success)
                {
                    LogInfo($"Successfully restored comment from spam: {commentId}");
                }
                else
                {
                    LogError($"Failed to restore comment from spam: {commentId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogError($"Error restoring comment from spam {commentId}: {ex.Message}");
                throw new Exception($"Không thể khôi phục bình luận: {ex.Message}", ex);
            }
        }

        #endregion

        #region Private Helper Methods

        private void ValidateActivityId(string activityId)
        {
            if (string.IsNullOrWhiteSpace(activityId))
            {
                throw new ArgumentException("Activity ID không được để trống", nameof(activityId));
            }
        }

        private void ValidateCommentId(string commentId)
        {
            if (string.IsNullOrWhiteSpace(commentId))
            {
                throw new ArgumentException("Comment ID không được để trống", nameof(commentId));
            }
        }

        private void ValidateContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Nội dung bình luận không được để trống", nameof(content));
            }

            if (content.Trim().Length > 1000)
            {
                throw new ArgumentException("Nội dung bình luận không được vượt quá 1000 ký tự", nameof(content));
            }
        }

        private void ValidateCreateCommentRequest(CreateCommentRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "Request không được null");
            }

            ValidateActivityId(request.ActivityId);
            ValidateContent(request.Content);

            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                throw new ArgumentException("User ID không được để trống", nameof(request.UserId));
            }

            // Validate ParentCommentId if provided
            if (!string.IsNullOrWhiteSpace(request.ParentCommentId))
            {
                if (!MongoDB.Bson.ObjectId.TryParse(request.ParentCommentId.Trim(), out _))
                {
                    throw new ArgumentException("ParentCommentId không đúng định dạng", nameof(request.ParentCommentId));
                }
            }
        }

        private string GetCurrentUserId()
        {
            try
            {
                // Try getting from App properties first
                if (System.Windows.Application.Current?.Properties.Contains("CurrentUser") == true &&
                    System.Windows.Application.Current.Properties["CurrentUser"] is User currentUser)
                {
                    return currentUser.Id;
                }

                // Fallback to settings
                return DoanKhoaClient.Properties.Settings.Default.CurrentUserId ?? "";
            }
            catch (Exception ex)
            {
                LogError($"Error getting current user ID: {ex.Message}");
                return "";
            }
        }

        private async Task EnsureSuccessResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"API call failed with status {response.StatusCode}: {errorContent}";
                LogError(errorMessage);
                throw new HttpRequestException(errorMessage);
            }
        }

        private Comment MapToComment(CommentResponse response)
        {
            if (response == null)
            {
                LogWarning("Attempting to map null CommentResponse");
                return null;
            }

            try
            {
                var comment = new Comment
                {
                    Id = response.Id ?? "",
                    ActivityId = response.ActivityId ?? "",
                    UserId = response.UserId ?? "",
                    UserDisplayName = response.UserDisplayName ?? "Unknown User",
                    UserAvatar = !string.IsNullOrEmpty(response.UserAvatar) ? response.UserAvatar : "/Views/Images/User-icon.png",
                    Content = response.Content ?? "",
                    CreatedAt = response.CreatedAt,
                    UpdatedAt = response.UpdatedAt,
                    ParentCommentId = response.ParentCommentId, // Quan trọng: đảm bảo ParentCommentId được set
                    LikeCount = response.LikeCount,
                    IsLiked = response.IsLiked,
                    IsOwner = response.IsOwner,
                    // Thông tin về comment được phản hồi - sẽ được populate sau
                    ReplyToUserName = response.ReplyToUserName,
                    ReplyToUserId = response.ReplyToUserId,
                    ReplyToContent = response.ReplyToContent
                };

                // Debug log
                System.Diagnostics.Debug.WriteLine($"=== MAPPING COMMENT ===");
                System.Diagnostics.Debug.WriteLine($"ID: {comment.Id}");
                System.Diagnostics.Debug.WriteLine($"Content: {comment.Content}");
                System.Diagnostics.Debug.WriteLine($"ParentCommentId: {comment.ParentCommentId ?? "null"}");
                System.Diagnostics.Debug.WriteLine($"IsReply: {comment.IsReply}");
                System.Diagnostics.Debug.WriteLine($"ReplyToUserName: {comment.ReplyToUserName ?? "null"}");

                // Đặt IsOwner dựa trên user hiện tại nếu chưa có
                if (!comment.IsOwner)
                {
                    var currentUserId = GetCurrentUserId();
                    comment.IsOwner = !string.IsNullOrEmpty(currentUserId) && comment.UserId == currentUserId;
                }

                return comment;
            }
            catch (Exception ex)
            {
                LogError($"Error mapping CommentResponse to Comment: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Logging Methods

        private void LogRequest(string message)
        {
            Debug.WriteLine($"[CommentService Request] {message}");
        }

        private void LogResponse(string message)
        {
            Debug.WriteLine($"[CommentService Response] {message}");
        }

        private void LogInfo(string message)
        {
            Debug.WriteLine($"[CommentService Info] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.WriteLine($"[CommentService Warning] {message}");
        }

        private void LogError(string message)
        {
            Debug.WriteLine($"[CommentService Error] {message}");
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _httpClient?.Dispose();
                }
                catch (Exception ex)
                {
                    LogError($"Error disposing CommentService: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        ~CommentService()
        {
            Dispose(false);
        }

        #endregion
    }
}