// TypeScript interfaces for the API responses
export interface TagTreeResponse {
  tagId: number;
  userId: number;
  name: string;
  parentId?: number;
  path?: string;
  slug?: string;
  color?: string;
  icon?: string;
  accessType: 'owner' | 'shared';
  level: number;
  usageCount: number;
  childrenCount: number;
  children: TagTreeResponse[];
  isExpanded: boolean;
  isSelected: boolean;
}

export interface ApiResponse<T> {
  data?: T;
  success: boolean;
  message?: string;
  errors?: string[];
}

// Tag Tree API Service
export class TagTreeService {
  private readonly baseUrl: string;

  constructor(baseUrl: string = '/api') {
    this.baseUrl = baseUrl;
  }

  /**
   * Fetches hierarchical tag tree from the API
   * @param includeShared - Whether to include shared tags from other users
   * @returns Promise containing the tag tree
   */
  async getTagTree(includeShared: boolean = true): Promise<TagTreeResponse[]> {
    try {
      const url = `${this.baseUrl}/tags/tree?includeShared=${includeShared}`;
      
      const response = await fetch(url, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          // Add authentication header if needed
          // 'Authorization': `Bearer ${token}`
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data: TagTreeResponse[] = await response.json();
      return data;
    } catch (error) {
      console.error('Error fetching tag tree:', error);
      throw error;
    }
  }

  /**
   * Fetches regular flat list of tags
   * @returns Promise containing the flat tag list
   */
  async getTags(): Promise<any[]> {
    try {
      const response = await fetch(`${this.baseUrl}/tags`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error('Error fetching tags:', error);
      throw error;
    }
  }
}

// React Hook for using Tag Tree (if using React)
export function useTagTree() {
  const [tagTree, setTagTree] = useState<TagTreeResponse[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  const tagTreeService = new TagTreeService();

  const fetchTagTree = async (includeShared: boolean = true) => {
    setLoading(true);
    setError(null);
    
    try {
      const data = await tagTreeService.getTagTree(includeShared);
      setTagTree(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch tag tree');
    } finally {
      setLoading(false);
    }
  };

  const toggleTagExpansion = (tagId: number) => {
    setTagTree(prevTree => 
      toggleExpansionRecursively(prevTree, tagId)
    );
  };

  const selectTag = (tagId: number) => {
    setTagTree(prevTree => 
      selectTagRecursively(prevTree, tagId)
    );
  };

  return {
    tagTree,
    loading,
    error,
    fetchTagTree,
    toggleTagExpansion,
    selectTag,
  };
}

// Utility functions for tree manipulation
function toggleExpansionRecursively(
  tags: TagTreeResponse[], 
  targetId: number
): TagTreeResponse[] {
  return tags.map(tag => {
    if (tag.tagId === targetId) {
      return { ...tag, isExpanded: !tag.isExpanded };
    }
    
    if (tag.children.length > 0) {
      return {
        ...tag,
        children: toggleExpansionRecursively(tag.children, targetId)
      };
    }
    
    return tag;
  });
}

function selectTagRecursively(
  tags: TagTreeResponse[], 
  targetId: number
): TagTreeResponse[] {
  return tags.map(tag => {
    const isSelected = tag.tagId === targetId;
    
    return {
      ...tag,
      isSelected,
      children: tag.children.length > 0 
        ? selectTagRecursively(tag.children, targetId)
        : tag.children
    };
  });
}

// Vue.js Composable (if using Vue 3)
export function useTagTreeVue() {
  const tagTree = ref<TagTreeResponse[]>([]);
  const loading = ref<boolean>(false);
  const error = ref<string | null>(null);

  const tagTreeService = new TagTreeService();

  const fetchTagTree = async (includeShared: boolean = true) => {
    loading.value = true;
    error.value = null;
    
    try {
      const data = await tagTreeService.getTagTree(includeShared);
      tagTree.value = data;
    } catch (err) {
      error.value = err instanceof Error ? err.message : 'Failed to fetch tag tree';
    } finally {
      loading.value = false;
    }
  };

  const toggleTagExpansion = (tagId: number) => {
    tagTree.value = toggleExpansionRecursively(tagTree.value, tagId);
  };

  const selectTag = (tagId: number) => {
    tagTree.value = selectTagRecursively(tagTree.value, tagId);
  };

  return {
    tagTree: readonly(tagTree),
    loading: readonly(loading),
    error: readonly(error),
    fetchTagTree,
    toggleTagExpansion,
    selectTag,
  };
}

// Plain JavaScript version (no framework)
export class TagTreeManager {
  private tagTree: TagTreeResponse[] = [];
  private loading: boolean = false;
  private error: string | null = null;
  private onStateChange?: (state: { tagTree: TagTreeResponse[], loading: boolean, error: string | null }) => void;

  constructor(onStateChange?: (state: { tagTree: TagTreeResponse[], loading: boolean, error: string | null }) => void) {
    this.onStateChange = onStateChange;
  }

  private notifyStateChange() {
    if (this.onStateChange) {
      this.onStateChange({
        tagTree: [...this.tagTree],
        loading: this.loading,
        error: this.error
      });
    }
  }

  async fetchTagTree(includeShared: boolean = true): Promise<void> {
    this.loading = true;
    this.error = null;
    this.notifyStateChange();
    
    try {
      const tagTreeService = new TagTreeService();
      const data = await tagTreeService.getTagTree(includeShared);
      this.tagTree = data;
    } catch (err) {
      this.error = err instanceof Error ? err.message : 'Failed to fetch tag tree';
    } finally {
      this.loading = false;
      this.notifyStateChange();
    }
  }

  toggleTagExpansion(tagId: number): void {
    this.tagTree = toggleExpansionRecursively(this.tagTree, tagId);
    this.notifyStateChange();
  }

  selectTag(tagId: number): void {
    this.tagTree = selectTagRecursively(this.tagTree, tagId);
    this.notifyStateChange();
  }

  getTagTree(): TagTreeResponse[] {
    return [...this.tagTree];
  }

  isLoading(): boolean {
    return this.loading;
  }

  getError(): string | null {
    return this.error;
  }
}

// Example usage functions
export const TagTreeExamples = {
  // Basic usage example
  async basicUsage() {
    const tagTreeService = new TagTreeService();
    
    try {
      // Fetch tag tree with shared tags
      const tagTree = await tagTreeService.getTagTree(true);
      console.log('Tag tree:', tagTree);
      
      // Fetch only owned tags
      const ownedTags = await tagTreeService.getTagTree(false);
      console.log('Owned tags only:', ownedTags);
      
    } catch (error) {
      console.error('Error:', error);
    }
  },

  // React component example
  reactComponentExample: `
    import React, { useEffect } from 'react';
    import { useTagTree } from './tagTreeService';

    const TagTreeComponent = () => {
      const { tagTree, loading, error, fetchTagTree, toggleTagExpansion, selectTag } = useTagTree();

      useEffect(() => {
        fetchTagTree(true); // Include shared tags
      }, []);

      const renderTag = (tag) => (
        <div key={tag.tagId} style={{ marginLeft: tag.level * 20 }}>
          <div 
            onClick={() => toggleTagExpansion(tag.tagId)}
            style={{ 
              cursor: 'pointer',
              backgroundColor: tag.isSelected ? '#e3f2fd' : 'transparent',
              padding: '5px',
              borderRadius: '3px'
            }}
          >
            {tag.childrenCount > 0 && (
              <span>{tag.isExpanded ? '?' : '?'} </span>
            )}
            <span 
              style={{ color: tag.color || '#000' }}
              onClick={(e) => { e.stopPropagation(); selectTag(tag.tagId); }}
            >
              {tag.name}
            </span>
            <small> ({tag.usageCount} uses)</small>
            {tag.accessType === 'shared' && <span> ??</span>}
          </div>
          
          {tag.isExpanded && tag.children.map(renderTag)}
        </div>
      );

      if (loading) return <div>Loading tags...</div>;
      if (error) return <div>Error: {error}</div>;

      return (
        <div>
          <h3>Tag Tree</h3>
          {tagTree.map(renderTag)}
        </div>
      );
    };

    export default TagTreeComponent;
  `,

  // Vue component example
  vueComponentExample: `
    <template>
      <div>
        <h3>Tag Tree</h3>
        <div v-if="loading">Loading tags...</div>
        <div v-else-if="error">Error: {{ error }}</div>
        <div v-else>
          <TagTreeNode 
            v-for="tag in tagTree" 
            :key="tag.tagId"
            :tag="tag"
            @toggle-expansion="toggleTagExpansion"
            @select-tag="selectTag"
          />
        </div>
      </div>
    </template>

    <script setup lang="ts">
    import { onMounted } from 'vue';
    import { useTagTreeVue } from './tagTreeService';

    const { tagTree, loading, error, fetchTagTree, toggleTagExpansion, selectTag } = useTagTreeVue();

    onMounted(() => {
      fetchTagTree(true); // Include shared tags
    });
    </script>
  `
};