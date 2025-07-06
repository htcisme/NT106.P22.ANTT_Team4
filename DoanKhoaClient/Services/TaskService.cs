using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.Diagnostics;
using MongoDB.Bson;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DoanKhoaClient.Services
{
    public delegate void TaskItemsRefreshedHandler(object sender, EventArgs e);

    public class TaskService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        // Các event để thông báo khi dữ liệu được cập nhật
        public delegate void TaskSessionUpdatedHandler(TaskSession session);
        public event TaskSessionUpdatedHandler TaskSessionUpdated;

        public delegate void TaskProgramUpdatedHandler(TaskProgram program);
        public event TaskProgramUpdatedHandler TaskProgramUpdated;

        public delegate void TaskItemUpdatedHandler(TaskItem item);
        public event TaskItemUpdatedHandler TaskItemUpdated;

        public event TaskItemsRefreshedHandler TaskItemsRefreshed;
        public TaskService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5299/api/")
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        private void OnTaskSessionUpdated(TaskSession session)
        {
            TaskSessionUpdated?.Invoke(session);
        }

        protected virtual void OnTaskProgramUpdated(TaskProgram program)
        {
            TaskProgramUpdated?.Invoke(program);
        }


        private void OnTaskItemUpdated(TaskItem item)
        {
            TaskItemUpdated?.Invoke(item);
        }

        // Các phương thức CRUD cho TaskSession
        public async Task<List<TaskSession>> GetTaskSessionsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("tasksession");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<TaskSession>>();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi lấy danh sách phiên làm việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return new List<TaskSession>();
            }
        }
        public async Task<TaskSession> CreateTaskSessionAsync(TaskSession session)
        {
            try
            {
                // Đảm bảo các trường thời gian được cập nhật
                session.CreatedAt = DateTime.UtcNow;
                session.UpdatedAt = DateTime.UtcNow;

                // Log dữ liệu gửi đi để debug
                string requestJson = Newtonsoft.Json.JsonConvert.SerializeObject(session, Newtonsoft.Json.Formatting.Indented);
                Console.WriteLine($"Đang gửi request tạo session: {requestJson}");

                var response = await _httpClient.PostAsJsonAsync("tasksession", session);

                // Log response
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response từ server: {response.StatusCode}");
                Console.WriteLine($"Response content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Server trả về lỗi: {response.StatusCode}\nChi tiết: {responseContent}",
                        "Lỗi tạo phiên làm việc", MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new Exception($"Server trả về lỗi: {response.StatusCode}, Details: {responseContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<TaskSession>();
                OnTaskSessionUpdated(result);
                return result;
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.WriteLine($"Lỗi trong CreateTaskSessionAsync: {ex.Message}");
                MessageBox.Show($"Lỗi khi tạo phiên làm việc: {ex.Message}", "Lỗi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        public async Task<TaskSession> UpdateTaskSessionAsync(string id, TaskSession session)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"tasksession/{id}", session);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<TaskSession>();
                OnTaskSessionUpdated(result);
                return result;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi cập nhật phiên làm việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }

        public async Task<TaskProgram> GetTaskProgramByIdAsync(string programId)
        {
            try
            {
                Debug.WriteLine($"===== GET TASK PROGRAM BY ID REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskprogram/{programId}");

                var response = await _httpClient.GetAsync($"taskprogram/{programId}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== GET TASK PROGRAM BY ID RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<TaskProgram>();
                    return result;
                }
                else
                {
                    Debug.WriteLine($"API Error: {response.StatusCode}, Content: {responseContent}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== GET TASK PROGRAM BY ID EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }
        public async Task DeleteTaskSessionAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"tasksession/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi xóa phiên làm việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }

        // Các phương thức CRUD cho TaskProgram
        public async Task<List<TaskProgram>> GetTaskProgramsAsync(string sessionId)
        {
            try
            {
                Debug.WriteLine($"===== GET TASK PROGRAMS REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskprogram/session/{sessionId}");

                var response = await _httpClient.GetAsync($"taskprogram/session/{sessionId}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== GET TASK PROGRAMS RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<TaskProgram>>();
                    return result ?? new List<TaskProgram>();
                }
                else
                {
                    Debug.WriteLine($"API Error: {response.StatusCode}, Content: {responseContent}");
                    return new List<TaskProgram>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== GET TASK PROGRAMS EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<TaskProgram>();
            }
        }
        public async Task<TaskProgram> CreateTaskProgramAsync(TaskProgram program)
        {
            try
            {
                // Ensure we're not sending duplicate requests
                await _semaphore.WaitAsync(); // Add a semaphore to prevent concurrent calls

                try
                {
                    // Basic validation to ensure we don't create duplicates
                    if (!string.IsNullOrEmpty(program.Id))
                    {
                        // Check if this program already exists
                        var existing = await GetTaskProgramByIdAsync(program.Id);
                        if (existing != null)
                        {
                            return existing; // Return the existing program instead of creating a duplicate
                        }
                    }

                    // Đảm bảo các trường thời gian được cập nhật
                    program.CreatedAt = DateTime.Now;
                    program.UpdatedAt = DateTime.Now;

                    // Set up executor details
                    if (string.IsNullOrEmpty(program.ExecutorId))
                    {
                        program.ExecutorId = program.SessionId;
                    }

                    if (string.IsNullOrEmpty(program.ExecutorName))
                    {
                        program.ExecutorName = "Auto Assigned";
                    }

                    // Don't send the ID - let the server generate it
                    var programToCreate = new TaskProgram
                    {
                        Id = ObjectId.GenerateNewId().ToString(), // Generate a new ID
                        Name = program.Name,
                        Description = program.Description,
                        StartDate = program.StartDate,
                        EndDate = program.EndDate,
                        SessionId = program.SessionId,
                        ExecutorId = program.ExecutorId,
                        ExecutorName = program.ExecutorName,
                        Type = program.Type,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        Status = ProgramStatus.NotStarted
                    };

                    // Send request
                    var response = await _httpClient.PostAsJsonAsync("taskprogram", programToCreate);

                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"API Error ({response.StatusCode}): {errorContent}");
                    }

                    var result = await response.Content.ReadFromJsonAsync<TaskProgram>();
                    OnTaskProgramUpdated(result);
                    return result;
                }
                finally
                {
                    _semaphore.Release(); // Release the semaphore when done
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi tạo chương trình: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }

        public async Task<TaskProgram> UpdateTaskProgramAsync(string id, TaskProgram program)
        {
            try
            {
                // Đảm bảo cập nhật thời gian
                program.UpdatedAt = DateTime.Now;

                // Đảm bảo ExecutorId và ExecutorName luôn có giá trị
                if (string.IsNullOrEmpty(program.ExecutorId))
                {
                    program.ExecutorId = program.SessionId;
                }

                if (string.IsNullOrEmpty(program.ExecutorName))
                {
                    program.ExecutorName = "Auto Assigned";
                }

                // Log request để debug
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = System.Text.Json.JsonSerializer.Serialize(program, jsonOptions);
                Debug.WriteLine($"Updating program: {jsonContent}");

                var response = await _httpClient.PutAsJsonAsync($"taskprogram/{id}", program);

                // Kiểm tra response
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"API Error: {response.StatusCode}, Details: {errorContent}");
                    throw new Exception($"API Error ({response.StatusCode}): {errorContent}");
                }

                // If NoContent (204), return the original program with updated id
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    // Just use the program that was passed in since it was successfully updated
                    OnTaskProgramUpdated(program);
                    return program;
                }
                else
                {
                    // Otherwise try to deserialize the response
                    var result = await response.Content.ReadFromJsonAsync<TaskProgram>();
                    OnTaskProgramUpdated(result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in UpdateTaskProgramAsync: {ex}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi cập nhật chương trình: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });

                // Trong trường hợp API chưa sẵn sàng
                return program;
            }
        }

        public async Task<bool> SendTaskReminderAsync(string taskItemId)
        {
            try
            {
                Debug.WriteLine($"Sending manual reminder for task: {taskItemId}");

                var response = await _httpClient.PostAsync($"taskitem/{taskItemId}/send-reminder", null);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<dynamic>();
                    Debug.WriteLine($"✅ Reminder sent successfully");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"❌ Failed to send reminder: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception sending reminder: {ex.Message}");
                return false;
            }
        }

        // ✅ THÊM: Test email
        public async Task<bool> SendTestEmailAsync(string email, string name, string type)
        {
            try
            {
                Debug.WriteLine($"Sending test email: {type} to {email}");

                var request = new
                {
                    Email = email,
                    Name = name,
                    Type = type
                };

                var response = await _httpClient.PostAsJsonAsync("taskitem/test-email", request);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"✅ Test email sent successfully");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"❌ Failed to send test email: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception sending test email: {ex.Message}");
                return false;
            }
        }
        public async Task<List<TaskItem>> GetTaskItemsByProgramIdAsync(string programId)
        {
            try
            {
                Debug.WriteLine($"===== TaskService.GetTaskItemsByProgramIdAsync =====");
                Debug.WriteLine($"ProgramId: {programId}");

                var response = await _httpClient.GetAsync($"taskitem/program/{programId}");
                Debug.WriteLine($"Server response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Raw server response:\n{responseContent}");

                    var taskItems = await response.Content.ReadFromJsonAsync<List<TaskItem>>();

                    Debug.WriteLine($"✅ Parsed {taskItems?.Count ?? 0} task items:");
                    if (taskItems != null)
                    {
                        foreach (var item in taskItems)
                        {
                            Debug.WriteLine($"  - '{item.Title}' - {item.AssignedToName} ({item.AssignedToEmail}) - Due: {item.DueDate:yyyy-MM-dd}");
                        }
                    }

                    return taskItems ?? new List<TaskItem>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"❌ Server error: {response.StatusCode}");
                    Debug.WriteLine($"Error content: {errorContent}");
                    throw new HttpRequestException($"Server returned error: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in GetTaskItemsByProgramIdAsync: {ex.Message}");
                throw;
            }
        }
        // Thêm vào TaskService.cs
        public async Task<List<TaskItem>> GetAllActiveTaskItemsAsync()
        {
            try
            {
                Debug.WriteLine($"===== GET ALL ACTIVE TASK ITEMS REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskitem/active");

                var response = await _httpClient.GetAsync("taskitem/active");

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== GET ALL ACTIVE TASK ITEMS RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<TaskItem>>();
                    return result ?? new List<TaskItem>();
                }
                else
                {
                    Debug.WriteLine($"API Error: {response.StatusCode}, Content: {responseContent}");
                    return new List<TaskItem>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== GET ALL ACTIVE TASK ITEMS EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<TaskItem>();
            }
        }
        // Cập nhật phương thức UpdateTaskProgramAsync để hỗ trợ người thực hiện
        public async Task DeleteTaskProgramAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"taskprogram/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi xóa chương trình: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }

        public async Task<List<TaskItem>> GetTaskItemsAsync(string programId)
        {
            try
            {
                // Đảm bảo sử dụng endpoint chính xác và nhất quán
                var response = await _httpClient.GetAsync($"taskitem/program/{programId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<TaskItem>>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting task items: {ex.Message}");
                // Trả về danh sách rỗng thay vì ném ngoại lệ để tránh crash
                return new List<TaskItem>();
            }
        }
        public async Task<List<GroupContent>> GetGroupContentsAsync(string sessionId)
        {
            try
            {
                Debug.WriteLine($"===== GET GROUP CONTENTS REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}groupcontent/session/{sessionId}");

                var response = await _httpClient.GetAsync($"groupcontent/session/{sessionId}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== GET GROUP CONTENTS RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<GroupContent>>();
                    return result ?? new List<GroupContent>();
                }
                else
                {
                    Debug.WriteLine($"API Error: {response.StatusCode}, Content: {responseContent}");
                    return new List<GroupContent>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== GET GROUP CONTENTS EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi lấy danh sách nhóm công việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return new List<GroupContent>();
            }
        }

        public async Task<List<TaskItem>> GetTaskItemsByGroupContentAsync(string groupContentId)
        {
            try
            {
                Debug.WriteLine($"===== GET TASK ITEMS BY GROUP CONTENT REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskitem/groupcontent/{groupContentId}");

                var response = await _httpClient.GetAsync($"taskitem/groupcontent/{groupContentId}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== GET TASK ITEMS BY GROUP CONTENT RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<TaskItem>>();
                    return result ?? new List<TaskItem>();
                }
                else
                {
                    Debug.WriteLine($"API Error: {response.StatusCode}, Content: {responseContent}");
                    return new List<TaskItem>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== GET TASK ITEMS BY GROUP CONTENT EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<TaskItem>();
            }
        }
        public async Task<List<TaskItem>> GetTaskItemsByProgramAsync(string programId)
        {
            try
            {
                Debug.WriteLine($"===== GET TASK ITEMS BY PROGRAM REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskitem/program/{programId}");

                var response = await _httpClient.GetAsync($"taskitem/program/{programId}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== GET TASK ITEMS BY PROGRAM RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<List<TaskItem>>();
                    return result ?? new List<TaskItem>();
                }
                else
                {
                    Debug.WriteLine($"API Error: {response.StatusCode}, Content: {responseContent}");
                    return new List<TaskItem>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== GET TASK ITEMS BY PROGRAM EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                return new List<TaskItem>();
            }
        }
        public async Task<TaskItem> CreateTaskItemAsync(TaskItem taskItem)
        {
            // Thêm semaphore để tránh trùng lặp request
            await _semaphore.WaitAsync();

            try
            {
                // Đảm bảo taskItem có ID trước khi gửi lên server
                if (string.IsNullOrEmpty(taskItem.Id))
                {
                    taskItem.Id = ObjectId.GenerateNewId().ToString(); // Tạo ID mới
                }

                // Đảm bảo các trường ngày tháng được cập nhật
                taskItem.CreatedAt = DateTime.Now;
                taskItem.UpdatedAt = DateTime.Now;

                // Log thông tin chi tiết trước khi gửi request
                var requestJson = JsonConvert.SerializeObject(taskItem, Formatting.Indented);
                Debug.WriteLine($"===== CREATE TASK ITEM REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskitem");
                Debug.WriteLine($"Payload: {requestJson}");

                // Thực hiện request
                var response = await _httpClient.PostAsJsonAsync("taskitem", taskItem);

                // Log response
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== CREATE TASK ITEM RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Content: {responseContent}");

                // Xử lý response
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<TaskItem>(responseContent);
                    OnTaskItemUpdated(result);
                    return result;
                }
                else
                {
                    throw new Exception($"Server returned error: {response.StatusCode}, Message: {responseContent}");
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"===== CREATE TASK ITEM EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }
        public async Task<BulkReminderResponse> SendBulkTaskRemindersAsync(List<string> taskItemIds)
        {
            try
            {
                Debug.WriteLine($"===== Sending bulk reminders for {taskItemIds?.Count ?? 0} tasks =====");

                if (taskItemIds == null || !taskItemIds.Any())
                {
                    throw new ArgumentException("Danh sách TaskItem không được để trống");
                }

                foreach (var id in taskItemIds)
                {
                    Debug.WriteLine($"  - TaskItem ID: {id}");
                }

                var request = new BulkReminderRequest
                {
                    TaskItemIds = taskItemIds
                };

                var response = await _httpClient.PostAsJsonAsync("taskitem/send-bulk-reminders", request);

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Server response: {response.StatusCode}");
                Debug.WriteLine($"Response content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<BulkReminderResponse>();
                    Debug.WriteLine($"✅ Bulk reminders completed:");
                    Debug.WriteLine($"  - Total: {result.TotalProcessed}");
                    Debug.WriteLine($"  - Success: {result.SuccessCount}");
                    Debug.WriteLine($"  - Failed: {result.FailCount}");

                    return result;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"❌ Failed to send bulk reminders: {error}");
                    throw new HttpRequestException($"Failed to send bulk reminders: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in SendBulkTaskRemindersAsync: {ex.Message}");
                throw;
            }
        }

        // ✅ THÊM: Bulk Reminder Models (Client)
        public class BulkReminderRequest
        {
            public List<string> TaskItemIds { get; set; }
        }

        public class BulkReminderResponse
        {
            public int TotalProcessed { get; set; }
            public int SuccessCount { get; set; }
            public int FailCount { get; set; }
            public List<BulkReminderResult> Results { get; set; }
        }

        public class BulkReminderResult
        {
            public string TaskItemId { get; set; }
            public bool Success { get; set; }
            public string Message { get; set; }
            public string TaskTitle { get; set; }
            public string AssignedToEmail { get; set; }
        }
        public async Task<TaskItem> UpdateTaskItemAsync(string id, TaskItem taskItem)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Task ID cannot be null or empty", nameof(id));
            }

            if (taskItem == null)
            {
                throw new ArgumentNullException(nameof(taskItem), "Task item cannot be null");
            }

            await _semaphore.WaitAsync();
            try
            {
                // Đảm bảo ID trong taskItem khớp với id tham số
                taskItem.Id = id;

                // Đảm bảo cập nhật thời gian
                taskItem.UpdatedAt = DateTime.Now;

                // Log payload để debug
                Debug.WriteLine($"===== UPDATE TASK ITEM REQUEST =====");
                Debug.WriteLine($"URL: {_httpClient.BaseAddress}taskitem/{id}");
                Debug.WriteLine($"Payload: {JsonConvert.SerializeObject(taskItem, Formatting.Indented)}");

                // Thực hiện request
                var response = await _httpClient.PutAsJsonAsync($"taskitem/{id}", taskItem);

                // Log response để debug
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== UPDATE TASK ITEM RESPONSE =====");
                Debug.WriteLine($"Status code: {(int)response.StatusCode} ({response.StatusCode})");
                Debug.WriteLine($"Response content: {responseContent}");

                // Xử lý response
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        // Sử dụng Newtonsoft.Json với cài đặt linh hoạt hơn
                        var settings = new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            Error = (sender, args) =>
                            {
                                Debug.WriteLine($"Serialize Error: {args.ErrorContext.Error.Message}");
                                args.ErrorContext.Handled = true;
                            }
                        };
                        var result = JsonConvert.DeserializeObject<TaskItem>(responseContent, settings);

                        if (result != null)
                        {
                            // Đảm bảo ID được giữ nguyên nếu bị thiếu trong response
                            if (string.IsNullOrEmpty(result.Id))
                            {
                                result.Id = id;
                            }

                            OnTaskItemUpdated(result);
                            return result;
                        }
                        else
                        {
                            // Nếu deserialization thất bại, sử dụng taskItem cũ với status cập nhật
                            Debug.WriteLine("Deserialization returned null, using original taskItem with updated timestamp");
                            taskItem.UpdatedAt = DateTime.Now;
                            OnTaskItemUpdated(taskItem);
                            return taskItem;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Exception during deserialization: {ex.Message}");
                        Debug.WriteLine($"Response content: {responseContent}");
                        throw new Exception($"Failed to process server response: {ex.Message}");
                    }
                }
                else
                {
                    throw new Exception($"Server returned error: {response.StatusCode}, Message: {responseContent}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
        // Và DeleteTaskItemAsync:
        public async Task<bool> DeleteTaskItemAsync(string taskItemId)
        {
            try
            {
                Debug.WriteLine($"Deleting TaskItem: {taskItemId}");
                var response = await _httpClient.DeleteAsync($"taskitem/{taskItemId}");

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"✅ TaskItem deleted");
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"❌ Failed to delete TaskItem: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception deleting TaskItem: {ex.Message}");
                return false;
            }
        }
        public async Task<TaskItem> CompleteTaskItemAsync(string taskItemId)
        {
            try
            {
                Debug.WriteLine($"Completing TaskItem: {taskItemId}");
                var response = await _httpClient.PostAsync($"taskitem/{taskItemId}/complete", null);

                if (response.IsSuccessStatusCode)
                {
                    var completedItem = await response.Content.ReadFromJsonAsync<TaskItem>();
                    Debug.WriteLine($"✅ TaskItem completed");
                    return completedItem;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"❌ Failed to complete TaskItem: {error}");
                    throw new HttpRequestException($"Failed to complete task item: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception completing TaskItem: {ex.Message}");
                throw;
            }
        }
        // Thêm phương thức lấy task theo ID nếu chưa có
        public async Task<TaskItem> GetTaskItemAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"taskitem/{id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TaskItem>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting task item: {ex.Message}");
                return null;
            }
        }
    }
}