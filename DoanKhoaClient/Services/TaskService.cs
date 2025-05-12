using DoanKhoaClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.Diagnostics;

namespace DoanKhoaClient.Services
{
    public class TaskService
    {
        private readonly HttpClient _httpClient;

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
                // Chuyển đổi TaskSession sang DTO (không có trường ID)
                var dto = CreateTaskSessionDto.FromTaskSession(session);

                // Ghi log để debug
                Console.WriteLine($"Sending DTO: Name={dto.Name}, ManagerId={dto.ManagerId}, Type={dto.Type}");

                // Gửi DTO thay vì gửi toàn bộ đối tượng session
                var response = await _httpClient.PostAsJsonAsync("tasksession", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Server response: {response.StatusCode}, Details: {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<TaskSession>();
                OnTaskSessionUpdated(result);
                return result;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi tạo phiên làm việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
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
                // Đảm bảo các trường thời gian được cập nhật
                program.CreatedAt = DateTime.Now;
                program.UpdatedAt = DateTime.Now;

                // Đảm bảo ExecutorId và ExecutorName luôn có giá trị
                if (string.IsNullOrEmpty(program.ExecutorId))
                {
                    // Sử dụng SessionId làm ExecutorId mặc định nếu không có
                    program.ExecutorId = program.SessionId;
                }

                if (string.IsNullOrEmpty(program.ExecutorName))
                {
                    program.ExecutorName = "Auto Assigned";
                }

                // Tạo bản sao của chương trình để gửi đến API
                var programToCreate = new TaskProgram
                {
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

                // Log request để debug
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                string jsonContent = JsonSerializer.Serialize(programToCreate, jsonOptions);
                Debug.WriteLine($"Sending to API: {jsonContent}");

                // Gửi API request
                var response = await _httpClient.PostAsJsonAsync("taskprogram", programToCreate);

                // Kiểm tra response
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"API Error: {response.StatusCode}, Details: {errorContent}");
                    throw new Exception($"API Error ({response.StatusCode}): {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<TaskProgram>();
                OnTaskProgramUpdated(result); // Kích hoạt sự kiện cập nhật
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in CreateTaskProgramAsync: {ex}");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi tạo chương trình: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });

                // Trong trường hợp API chưa sẵn sàng, tạo một chương trình mẫu
                program.Id = Guid.NewGuid().ToString();
                return program;
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
                string jsonContent = JsonSerializer.Serialize(program, jsonOptions);
                Debug.WriteLine($"Updating program: {jsonContent}");

                var response = await _httpClient.PutAsJsonAsync($"taskprogram/{id}", program);

                // Kiểm tra response
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"API Error: {response.StatusCode}, Details: {errorContent}");
                    throw new Exception($"API Error ({response.StatusCode}): {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<TaskProgram>();
                OnTaskProgramUpdated(result);
                return result;
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

        // Các phương thức CRUD cho TaskItem
        public async Task<List<TaskItem>> GetTaskItemsAsync(string programId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"taskitem/program/{programId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<TaskItem>>();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi lấy danh sách công việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                return new List<TaskItem>();
            }
        }

        public async Task<TaskItem> CreateTaskItemAsync(TaskItem item)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("taskitem", item);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<TaskItem>();
                OnTaskItemUpdated(result);
                return result;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi tạo công việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }

        public async Task<TaskItem> UpdateTaskItemAsync(string id, TaskItem item)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"taskitem/{id}", item);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<TaskItem>();
                OnTaskItemUpdated(result);
                return result;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi cập nhật công việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }

        public async Task DeleteTaskItemAsync(string id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"taskitem/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi xóa công việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }

        public async Task<TaskItem> CompleteTaskItemAsync(string id)
        {
            try
            {
                // Đảm bảo rằng mọi tham chiếu đến TaskStatus đã được thay thế bằng TaskItemStatus
                var response = await _httpClient.PutAsync($"taskitem/{id}/complete", null);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<TaskItem>();
                OnTaskItemUpdated(result);
                return result;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi hoàn thành công việc: {ex.Message}", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
                throw;
            }
        }
    }
}