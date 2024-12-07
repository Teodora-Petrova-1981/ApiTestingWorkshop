using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ApiTests
{
    [TestFixture]
    public class BlogApiTests : IDisposable
    {
        private RestClient client;
        private string token;
        public void Dispose()
        {
            client?.Dispose();
        }

        [SetUp]
        public void Setup() 
        { 
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empry");

        }

        [Test, Order(1)]
        public void Test_GetAllBlogs()
        {
            var request = new RestRequest("blog", Method.Get);

            var response = client.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(response.Content, Is.Not.Empty, "Response content should not be empty");

                var blogs = JArray.Parse(response.Content);
                Console.WriteLine(blogs.ToString());

                Assert.That(blogs.Type, Is.EqualTo(JTokenType.Array), "Expected response content to be a JSON array");
                Assert.That(blogs.Count, Is.GreaterThan(0), "Expected at least one blog in the response");

                foreach (var blog in blogs)
                {
                    Assert.That(blog["title"]?.ToString(), Is.Not.Null.And.Not.Empty,"Blog title should not be null or empty");
                    Assert.That(blog["description"]?.ToString(), Is.Not.Null.And.Not.Empty,"Blog description should not be null or empty");
                    Assert.That(blog["author"]?.ToString(), Is.Not.Null.And.Not.Empty,"Blog author should not be null or empty");
                    Assert.That(blog["category"]?.ToString(), Is.Not.Null.And.Not.Empty, "Blog category should not be null or empty");
                }
            });
        }
        [Test, Order(2)]
        public void Test_AddBlog()
        {
            var request = new RestRequest("blog", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new
            {
                title = "New Blog",
                description = "New Blog Description",
                category = "Technology"
            });

            var response = client.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code Created (201)");
                Assert.That(response.Content, Is.Not.Empty, "Response content should not be empty");

                var content = JObject.Parse(response.Content);  
                Console.WriteLine(content.ToString());

                Assert.That(content["title"]?.ToString(), Is.EqualTo("New Blog"), "Blog title should match the input");
                Assert.That(content["description"]?.ToString(), Is.EqualTo("New Blog Description"), "Blog description should match the input");
                Assert.That(content["category"]?.ToString(), Is.EqualTo("Technology"), "Blog category should match the input");
                Assert.That(content["author"]?.ToString(), Is.Not.Null.And.Not.Empty, "Blog author should not be null or empty");
            });
        }
        [Test, Order(3)]
        public void Test_UpdateBlog()
        {
            var getRequest = new RestRequest("blog", Method.Get);   

            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Failed to retrieve blogs");
            Assert.That(getResponse.Content, Is.Not.Empty, "Get blogs response content is empty");

            var blogs = JArray.Parse(getResponse.Content);
            Console.WriteLine(blogs.ToString());
            var blogToUpdate = blogs.FirstOrDefault(b => b["title"]?.ToString() == "New Blog");
            Console.WriteLine(blogToUpdate?.ToString());

            Assert.That(blogToUpdate, Is.Not.Null, "Blog with title 'New Blog' not found");

            var blogId = blogToUpdate["_id"].ToString();

            var updateRequest = new RestRequest("blog/{id}", Method.Put);
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddUrlSegment("id", blogId);
            updateRequest.AddJsonBody(new
            {
                title = "Updated Blog Title",
                description = "Updated Blog Description",
                category = "Lifestyle"
            });
            var updateResponse = client.Execute(updateRequest);

            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(updateResponse.Content, Is.Not.Empty, "Update response content should not be empty");

                var content = JObject.Parse(updateResponse.Content);
                Console.Write(content.ToString());

                Assert.That(content["title"]?.ToString(), Is.EqualTo("Updated Blog Title"), "Blog title should match the updated value");
                Assert.That(content["description"]?.ToString(), Is.EqualTo("Updated Blog Description"), "Blog description should match the updated value");
                Assert.That(content["category"]?.ToString(), Is.EqualTo("Lifestyle"), "Blog category should match the updated value");
                Assert.That(content["author"]?.ToString(), Is.Not.Null.And.Not.Empty, "Blog author should not be null or empty");
            });
        }
        [Test, Order(4)]
        public void Test_DeleteBlog()
        {
            var getRequest = new RestRequest("blog", Method.Get);
            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Failed to retrive blogs");
            Assert.That(getResponse.Content, Is.Not.Empty, "Get blogs response content is empty");

            var blogs = JArray.Parse(getResponse.Content);
            var blogToDelete = blogs.FirstOrDefault(b => b["title"]?.ToString() == "Updated Blog Title");

            Assert.That(blogToDelete, Is.Not.Null, "Blog with title 'Updated Blog Title' not found");

            Console.WriteLine(blogToDelete.ToString());
            var blogId = blogToDelete["_id"].ToString();

            var deleteRequest = new RestRequest("blog/{id}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");
            deleteRequest.AddUrlSegment("id", blogId);

            var deleteResponse = client.Execute(deleteRequest);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK");

                var verifyGetRequest = new RestRequest("blog/{id}", Method.Get);
                verifyGetRequest.AddUrlSegment("id", blogId);

                var verifyGetResponse = client.Execute(verifyGetRequest);

                Assert.That(verifyGetResponse.Content, Is.Null.Or.EqualTo("null"), "Verify get response content should be empty");
            });
        }
    }
}
