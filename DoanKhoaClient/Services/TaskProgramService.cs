using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;                    // ✅ THÊM: For HttpClient
using System.Net.Http.Json;               // ✅ THÊM: For ReadFromJsonAsync
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using DoanKhoaClient.Models;
namespace DoanKhoaClient.Services
{
    public class TaskProgramService
    {
        private readonly HttpClient _httpClient;

        public TaskProgramService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5299/api/");
        }
        public async Task<TaskProgram> GetTaskProgramByIdAsync(string programId)
        {
            try
            {
                Debug.WriteLine($"Getting TaskProgram by ID: {programId}");

                var response = await _httpClient.GetAsync($"taskprogram/{programId}");

                if (response.IsSuccessStatusCode)
                {
                    var program = await response.Content.ReadFromJsonAsync<TaskProgram>();
                    Debug.WriteLine($"✅ Retrieved TaskProgram: {program?.Name}");

                    // ✅ Also load TaskItems for this program
                    if (program != null)
                    {
                        var taskService = new TaskService();
                        var taskItems = await taskService.GetTaskItemsByProgramIdAsync(program.Id);

                        if (taskItems != null && taskItems.Count > 0)
                        {
                            program.TaskItems.Clear();
                            foreach (var task in taskItems)
                            {
                                program.TaskItems.Add(task);
                            }
                            Debug.WriteLine($"✅ Loaded {taskItems.Count} TaskItems into TaskProgram");
                        }
                    }

                    return program;
                }
                else
                {
                    Debug.WriteLine($"❌ TaskProgram not found: {programId} (Status: {response.StatusCode})");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting TaskProgram by ID: {ex.Message}");
                return null;
            }
        }

        // ✅ ADD: Get TaskProgram with TaskItems included
        public async Task<TaskProgram> GetTaskProgramWithTaskItemsAsync(string programId)
        {
            try
            {
                Debug.WriteLine($"Getting TaskProgram with TaskItems: {programId}");

                var response = await _httpClient.GetAsync($"taskprogram/{programId}/with-tasks");

                if (response.IsSuccessStatusCode)
                {
                    var program = await response.Content.ReadFromJsonAsync<TaskProgram>();
                    Debug.WriteLine($"✅ Retrieved TaskProgram with {program?.TaskItemsCount ?? 0} TaskItems");
                    return program;
                }
                else
                {
                    Debug.WriteLine($"❌ API endpoint not available, using fallback method");
                    return await GetTaskProgramByIdAsync(programId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error getting TaskProgram with TaskItems: {ex.Message}");
                return await GetTaskProgramByIdAsync(programId);
            }
        }
        public async Task<List<TaskProgram>> GetAllTaskProgramsAsync()
        {
            try
            {
                Debug.WriteLine("Getting all TaskPrograms from API");
                MessageBox.Show($"API Endpoint: {_httpClient.BaseAddress}taskprogram");

                // ✅ USE CORRECT ENDPOINT: /api/taskprogram
                var response = await _httpClient.GetAsync("taskprogram");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"API Response: {responseContent}");

                    var programs = await response.Content.ReadFromJsonAsync<List<TaskProgram>>();
                    Debug.WriteLine($"✅ Retrieved {programs?.Count ?? 0} TaskPrograms from API");

                    if (programs != null && programs.Count > 0)
                    {
                        Debug.WriteLine("=== TASK PROGRAMS FROM API ===");
                        foreach (var program in programs)
                        {
                            Debug.WriteLine($"Program: {program.Name}");
                            Debug.WriteLine($"  - ID: {program.Id}");
                            Debug.WriteLine($"  - SessionId: {program.SessionId}");
                            Debug.WriteLine($"  - Type: {program.Type} ({(int)program.Type})");
                            Debug.WriteLine($"  - Status: {program.Status} ({(int)program.Status})");
                            Debug.WriteLine($"  - ExecutorName: {program.ExecutorName}");
                            Debug.WriteLine($"  - StartDate: {program.StartDate:yyyy-MM-dd}");
                            Debug.WriteLine($"  - EndDate: {program.EndDate:yyyy-MM-dd}");
                            Debug.WriteLine("  ---");
                        }
                    }

                    return programs ?? new List<TaskProgram>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"❌ Failed to get TaskPrograms: {response.StatusCode}");
                    Debug.WriteLine($"Error content: {errorContent}");
                    return new List<TaskProgram>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Exception in GetAllTaskProgramsAsync: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return new List<TaskProgram>();
            }
        }
    }
}