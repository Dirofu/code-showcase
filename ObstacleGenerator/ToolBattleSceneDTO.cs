using System.Collections.Generic;

namespace Core.EditorTools.ObstacleGenerator
{
	public class ToolBattleSceneDTO
	{
		public int locationId;
		public string type;
	}

	public class ToolBattleSceneAnswerDTO
	{
		public List<ToolBattleSceneDTO> data;
	}
	
	public class ToolLocationSceneDTO
	{
		public int id;
		public string type;
	}
	
	public class ToolLocationSceneAnswerDTO
	{
		public List<ToolLocationSceneDTO> data;
	}
}
