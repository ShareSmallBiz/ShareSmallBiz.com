using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Infrastructure.Services
{


    // Service class implementation
    public class KeywordProvider(ShareSmallBizUserContext context)
    {
        // Get all keywords with counts from navigation properties
        public async Task<IEnumerable<KeywordModel>> GetAllKeywordsAsync()
        {
            return await context.Keywords
                .Include(i => i.Posts)
                .OrderBy(o => o.Name)
                .Select(k => GetKeywordModel(k))
                .ToListAsync().ConfigureAwait(false) ?? [];

        }

        // Get a single keyword by Id (including counts)
        public async Task<KeywordModel?> GetKeywordByIdAsync(int id)
        {
            var keyword = await context.Keywords
                .Include(k => k.Posts)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (keyword == null)
                return null;

            return GetKeywordModel(keyword);
        }

        // Create a new keyword from a model (only Name and Description are provided by the caller)
        public async Task<KeywordModel> CreateKeywordAsync(KeywordModel model)
        {
            // update model to remove any starting/end spaces, quote, or double quote
            model.Name = model.Name.Trim().Trim('"').Trim('\'');
            model.Description = model.Description.Trim().Trim('"').Trim('\'');

            // Check if name is already used
            var currentKeyword = await context.Keywords.FirstOrDefaultAsync(k => k.Name == model.Name);
            if (currentKeyword != null)
                return GetKeywordModel(currentKeyword);

            var keyword = new Keyword
            {
                Name = model.Name,
                Description = model.Description,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            context.Keywords.Add(keyword);
            await context.SaveChangesAsync();
            return GetKeywordModel(keyword);
        }

        private static KeywordModel GetKeywordModel(Keyword keyword)
        {

            // Return the newly created keyword with counts (which are zero initially)
            return new KeywordModel
            {
                Id = keyword.Id,
                Name = keyword.Name,
                Description = keyword.Description,
                PostCount = keyword.Posts.Count
            };
        }

        // Update an existing keyword identified by id with new Name and Description
        public async Task<KeywordModel?> UpdateKeywordAsync(int id, KeywordModel model)
        {


            var keyword = await context.Keywords.FindAsync(id);
            if (keyword == null)
                return null;

            // update model to remove any starting/end spaces, quote, or double quote
            model.Name = model.Name.Trim().Trim('"').Trim('\'');
            model.Description = model.Description.Trim().Trim('"').Trim('\'');

            keyword.Name = model.Name;
            keyword.Description = model.Description;
            keyword.ModifiedDate = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Reload the keyword including navigation properties to get updated counts
            var updatedKeyword = await context.Keywords
                .Include(k => k.Posts)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (updatedKeyword == null)
                return null;

            return new KeywordModel
            {
                Id = updatedKeyword.Id,
                Name = updatedKeyword.Name,
                Description = updatedKeyword.Description,
                PostCount = updatedKeyword.Posts.Count
            };
        }

        // Delete a keyword by its Id
        public async Task<bool> DeleteKeywordAsync(int id)
        {
            var keyword = await context.Keywords.FindAsync(id);
            if (keyword == null)
                return false;

            context.Keywords.Remove(keyword);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
