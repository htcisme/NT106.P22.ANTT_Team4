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

        public async Task<TaskProgram> GetTaskProgramByIdAsync(string id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"taskprogram/{id}");

                // If program is not found, return null instead of throwing exception
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<TaskProgram>();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi lấy thông tin chương trình: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
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
                var response = await _httpClient.GetAsync($"taskprogram/session/{sessionId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<TaskProgram>>();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi lấy danh sách chương trình: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
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
        public async Task<bool> DeleteTaskItemAsync(string id)
        {
            try
            {
                Debug.WriteLine($"===== DELETE TASK ITEM REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskitem/{id}");

                // Thực hiện request
                var response = await _httpClient.DeleteAsync($"taskitem/{id}");

                // Log response
                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== DELETE TASK ITEM RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Content: {responseContent}");

                // Xử lý response
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    throw new Exception($"Server returned error: {response.StatusCode}, Message: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== DELETE TASK ITEM EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw;
            }
        }
        public async Task<TaskItem> CompleteTaskItemAsync(string id)
        {
            try
            {
                Debug.WriteLine($"===== COMPLETE TASK ITEM REQUEST =====");
                Debug.WriteLine($"Endpoint: {_httpClient.BaseAddress}taskitem/{id}/complete");

                // Phương pháp 1: Thử sử dụng PUT thay vì POST
                HttpResponseMessage response;

                try
                {
                    // Tạo empty content với JSON thay vì null
                    var emptyContent = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
                    response = await _httpClient.PutAsync($"taskitem/{id}/complete", emptyContent);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"PUT method failed: {ex.Message}, trying alternative approach");

                    // Phương pháp 2: Cập nhật trạng thái task trực tiếp
                    var task = await GetTaskItemAsync(id);
                    if (task == null)
                    {
                        throw new Exception($"Task with ID {id} not found");
                    }

                    // Debug để xem giá trị Status hiện tại
                    Debug.WriteLine($"Current task status: {task.Status}");

                    // Trích xuất các giá trị enum có sẵn trong dự án
                    var statusType = task.Status.GetType();
                    Debug.WriteLine($"Status type: {statusType.FullName}");

                    // Liệt kê tất cả các giá trị enum có thể có
                    Debug.WriteLine("Available status values:");
                    foreach (var value in Enum.GetValues(statusType))
                    {
                        Debug.WriteLine($"- {value}");
                    }

                    // Tìm giá trị "Done" hoặc "Completed" trong enum
                    object doneValue = null;
                    foreach (var value in Enum.GetValues(statusType))
                    {
                        string valueName = value.ToString().ToLower();
                        if (valueName.Contains("done") || valueName.Contains("complete") || valueName.Contains("finish"))
                        {
                            doneValue = value;
                            break;
                        }
                    }

                    // Cập nhật trạng thái
                    if (doneValue != null)
                    {
                        // Thêm ép kiểu tường minh từ object sang TaskItemStatus
                        task.Status = (TaskItemStatus)doneValue;
                        Debug.WriteLine($"Setting status to: {doneValue}");
                    }
                    else
                    {
                        // Nếu không tìm thấy giá trị phù hợp, thử gán bằng số (thường 2 = Done)
                        try
                        {
                            // Thêm ép kiểu tường minh từ int sang TaskItemStatus
                            task.Status = (TaskItemStatus)2;
                            Debug.WriteLine("Setting status to numeric value: 2");
                        }
                        catch
                        {
                            Debug.WriteLine("Could not set status value");
                        }
                    }

                    task.UpdatedAt = DateTime.Now;
                    return await UpdateTaskItemAsync(id, task);
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"===== COMPLETE TASK ITEM RESPONSE =====");
                Debug.WriteLine($"Status: {response.StatusCode} ({(int)response.StatusCode})");
                Debug.WriteLine($"Content: {responseContent}");

                response.EnsureSuccessStatusCode();

                var result = JsonConvert.DeserializeObject<TaskItem>(responseContent);
                OnTaskItemUpdated(result);
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"===== COMPLETE TASK ITEM EXCEPTION =====");
                Debug.WriteLine($"Error: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Hiển thị thông báo lỗi chi tiết hơn
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi hoàn thành công việc: {ex.Message}",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                });

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