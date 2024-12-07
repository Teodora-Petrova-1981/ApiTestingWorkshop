using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ApiTests
{
    [TestFixture]
    public class BrandApiTests : IDisposable
    {
        private RestClient client;
        private string token;

        public void Dispose()
        {
            client?.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");

            Assert.That(token, Is.Not.Null.Or.Empty, "Authentication token should not be null or empty");
        }

        [Test, Order(1)]
        public void Test_GetAllBrands()
        {
            var request = new RestRequest("brand", Method.Get);

            var response = client.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(response.Content, Is.Not.Empty, "Response content should not be empty");

                var brands = JArray.Parse(response.Content);
                Console.WriteLine(brands.ToString());
                Assert.That(brands.Type, Is.EqualTo(JTokenType.Array), "Expected response content to be a JSON array");
                Assert.That(brands.Count, Is.GreaterThan(0), "Expected at least one brand in the response");

                var firstBrand = brands.FirstOrDefault();
                Assert.That(firstBrand, Is.Not.Null, "Expected at least one brand in the response");

                var brandNames = brands.Select(b => b["title"]?.ToString()).ToList();
                Assert.That(brandNames, Does.Contain("TechCorp"), "Expected brand title 'TechCorp'");
                Assert.That(brandNames, Does.Contain("GameMaster"), "Expected brand title 'GameMaster'");

                foreach (var brand in brands) 
                {
                    Assert.That(brand["_id"]?.ToString(), Is.Not.Null.And.Not.Empty, "Brand ID should not be null ot empty");
                    Assert.That(brand["title"]?.ToString(), Is.Not.Null.And.Not.Empty, "Brand title should not be null or empty");
                }
                Assert.That(brands.Count, Is.GreaterThan(5), "Expected more than 5 brands in the response");
            });
        }
        [Test, Order(2)]
        public void Test_AddBrand()
        {
            var request = new RestRequest("brand", Method.Post);
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new { title = "New Brand" });

            var response = client.Execute(request);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(response.Content, Is.Not.Empty, "Response content should not be empty");

                var content = JObject.Parse(response.Content);
                Console.WriteLine(content.ToString());

                Assert.That(content["_id"]?.ToString(), Is.Not.Null.And.Not.Empty, "Brand ID should not be null or empty");
                Assert.That(content["title"]?.ToString(), Is.EqualTo("New Brand"), "Brand title should match the input");
            });
        }
        [Test, Order(3)]
        public void Test_UpdateBrand()
        {
            var getRequest = new RestRequest("brand",Method.Get);

            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Failed to retrieve brands");
            Assert.That(getResponse.Content, Is.Not.Empty, "Get brands response content is empty");

            var brands = JArray.Parse(getResponse.Content);
            var brandToUpdate = brands.FirstOrDefault(b => b["title"]?.ToString() == "New Brand");

            Assert.That(brandToUpdate, Is.Not.Null, "Brand with title 'New Brand' not found");

            var brandId = brandToUpdate["_id"]?.ToString();

            var updateRequest = new RestRequest("brand/{id}",Method.Put);
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddUrlSegment("id",brandId);
            updateRequest.AddJsonBody(new { title = "Updated Brand Title" });
            Thread.Sleep(3000);

            var updateResponse = client.Execute(updateRequest);

            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code OK (200)");
                Assert.That(updateResponse.Content, Is.Not.Empty, "Response content should not be empty");

                var content = JObject.Parse(updateResponse.Content);
                Console.WriteLine(content);

                Assert.That(content["_id"]?.ToString(), Is.EqualTo(brandId), "Brand ID should match the updated brand's ID");
                Assert.That(content["title"]?.ToString(), Is.EqualTo("Updated Brand Title"), "Brand title should be updated correctly");

                Assert.That(content.ContainsKey("createdAt"), Is.True, "Brand should have a createdAt field");
                Assert.That(content.ContainsKey("updatedAt"), Is.True, "Brand should have an updatedAt field");

                Assert.That(content["updatedAt"]?.ToString(), Is.Not.EqualTo(content["createdAt"]?.ToString()), "updatedAt should be different from createdAt after an update");
            });
        }
        [Test, Order(4)]
        public void Test_DeleteBrand()
        {
            var getRequest = new RestRequest("brand", Method.Get);

            var getResponse = client.Execute(getRequest);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Failed to retrieve brands");
            Assert.That(getResponse.Content, Is.Not.Empty, "Get brands response content is empty");

            var brands = JArray.Parse(getResponse.Content);
            var brandToDelete = brands.FirstOrDefault(b => b["title"]?.ToString() == "Updated Brand Title");

            Assert.That(brandToDelete, Is.Not.Null, "Brand with title 'Updated Brand Title' not found");

            var brandId = brandToDelete["_id"]?.ToString();

            var deleteRequest = new RestRequest("brand/{id}", Method.Delete);
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");
            deleteRequest.AddUrlSegment("id", brandId);

            var deleteResponse = client.Execute(deleteRequest);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code NoContent (204) after deletion");

                var verifyGetRequest = new RestRequest("brand/{id}", Method.Get);
                verifyGetRequest.AddUrlSegment("id", brandId);
                var verifyGetResponse = client.Execute(verifyGetRequest);

                Assert.That(verifyGetResponse.Content, Is.Empty.Or.EqualTo("null"), "Verify get response content should be empty or 'null'");
            });
        }
    }
}
