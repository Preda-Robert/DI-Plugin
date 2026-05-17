using System;
using API.Entities;
using API.Interfaces;
using API.Repositories;

namespace API.Data;

public class RecipeInstructionRepository : BaseRepository<RecipeInstruction>, IRecipeInstructionRepository
{

    public RecipeInstructionRepository(DataContext context) : base(context)
    {
    }

}
