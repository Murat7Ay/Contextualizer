// Documentation Site JavaScript
class DocsApp {
    constructor() {
        this.init();
    }

    init() {
        this.setupNavigation();
        this.setupMobileMenu();
        this.setupSearch();
        this.setupScrollSpy();
        this.loadInitialSection();
    }

    setupNavigation() {
        const navLinks = document.querySelectorAll('.nav-link');
        const sections = document.querySelectorAll('.content-section');

        navLinks.forEach(link => {
            link.addEventListener('click', (e) => {
                e.preventDefault();
                const targetId = link.getAttribute('href').substring(1);
                const { sectionId, anchorId } = this.resolveSectionForAnchor(targetId);

                this.showSection(sectionId);
                this.setActiveNavLink(link);
                
                // Close mobile menu if open
                this.closeMobileMenu();
                
                // Update URL without scrolling
                history.pushState(null, null, `#${targetId}`);

                // Smooth scroll to sub-anchor if applicable
                if (anchorId && anchorId !== sectionId) {
                    const anchorEl = document.getElementById(anchorId);
                    if (anchorEl) {
                        anchorEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
                    }
                } else {
                    // Scroll to top of content for top-level sections
                    document.querySelector('.content-body').scrollTop = 0;
                }
            });
        });

        // Handle browser back/forward
        window.addEventListener('popstate', () => {
            const raw = window.location.hash.substring(1) || 'overview';
            const { sectionId, anchorId } = this.resolveSectionForAnchor(raw);
            this.showSection(sectionId);
            this.setActiveNavLinkById(raw);
            if (anchorId && anchorId !== sectionId) {
                const anchorEl = document.getElementById(anchorId);
                if (anchorEl) {
                    anchorEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            }
        });
    }

    setupMobileMenu() {
        const mobileMenuToggle = document.getElementById('mobileMenuToggle');
        const sidebar = document.querySelector('.sidebar');
        const overlay = document.createElement('div');
        
        overlay.className = 'mobile-overlay';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(0,0,0,0.5);
            z-index: 999;
            display: none;
        `;
        document.body.appendChild(overlay);

        mobileMenuToggle.addEventListener('click', () => {
            sidebar.classList.toggle('open');
            if (sidebar.classList.contains('open')) {
                overlay.style.display = 'block';
                document.body.style.overflow = 'hidden';
            } else {
                overlay.style.display = 'none';
                document.body.style.overflow = '';
            }
        });

        overlay.addEventListener('click', () => {
            this.closeMobileMenu();
        });

        // Close on escape key
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeMobileMenu();
            }
        });
    }

    closeMobileMenu() {
        const sidebar = document.querySelector('.sidebar');
        const overlay = document.querySelector('.mobile-overlay');
        
        sidebar.classList.remove('open');
        overlay.style.display = 'none';
        document.body.style.overflow = '';
    }

    setupSearch() {
        const searchInput = document.getElementById('searchInput');
        const searchResults = document.createElement('div');
        searchResults.className = 'search-results';
        searchResults.style.cssText = `
            position: absolute;
            top: 100%;
            left: 0;
            right: 0;
            background: white;
            border: 1px solid var(--border-color);
            border-top: none;
            border-radius: 0 0 6px 6px;
            max-height: 300px;
            overflow-y: auto;
            z-index: 1000;
            display: none;
        `;
        
        searchInput.parentElement.appendChild(searchResults);

        let searchTimeout;
        searchInput.addEventListener('input', (e) => {
            clearTimeout(searchTimeout);
            const query = e.target.value.trim();
            
            if (query.length < 2) {
                searchResults.style.display = 'none';
                return;
            }

            searchTimeout = setTimeout(() => {
                this.performSearch(query, searchResults);
            }, 300);
        });

        // Hide search results when clicking outside
        document.addEventListener('click', (e) => {
            if (!searchInput.parentElement.contains(e.target)) {
                searchResults.style.display = 'none';
            }
        });
    }

    performSearch(query, resultsContainer) {
        const sections = document.querySelectorAll('.content-section');
        const results = [];

        sections.forEach(section => {
            const sectionId = section.id;
            const sectionTitle = section.querySelector('h1')?.textContent || sectionId;
            const content = section.textContent.toLowerCase();
            
            if (content.includes(query.toLowerCase())) {
                // Find specific matches within the section
                const paragraphs = section.querySelectorAll('p, h2, h3, li');
                paragraphs.forEach(element => {
                    const text = element.textContent;
                    if (text.toLowerCase().includes(query.toLowerCase())) {
                        results.push({
                            sectionId,
                            sectionTitle,
                            text: this.highlightMatch(text, query),
                            element
                        });
                    }
                });
            }
        });

        this.displaySearchResults(results, resultsContainer, query);
    }

    highlightMatch(text, query) {
        const regex = new RegExp(`(${query})`, 'gi');
        return text.replace(regex, '<mark>$1</mark>');
    }

    displaySearchResults(results, container, query) {
        if (results.length === 0) {
            container.innerHTML = `
                <div style="padding: 1rem; text-align: center; color: var(--text-muted);">
                    No results found for "${query}"
                </div>
            `;
        } else {
            const uniqueResults = results.slice(0, 10); // Limit to 10 results
            container.innerHTML = uniqueResults.map(result => `
                <div class="search-result-item" style="
                    padding: 0.75rem 1rem;
                    border-bottom: 1px solid var(--border-color);
                    cursor: pointer;
                    transition: background-color 0.2s;
                " data-section="${result.sectionId}">
                    <div style="font-weight: 500; color: var(--primary-color); margin-bottom: 0.25rem;">
                        ${result.sectionTitle}
                    </div>
                    <div style="font-size: 0.875rem; color: var(--text-secondary);">
                        ${result.text.substring(0, 150)}...
                    </div>
                </div>
            `).join('');

            // Add click handlers to search results
            container.querySelectorAll('.search-result-item').forEach(item => {
                item.addEventListener('mouseenter', () => {
                    item.style.backgroundColor = 'var(--surface-color)';
                });
                item.addEventListener('mouseleave', () => {
                    item.style.backgroundColor = '';
                });
                item.addEventListener('click', () => {
                    const sectionId = item.dataset.section;
                    this.showSection(sectionId);
                    this.setActiveNavLinkById(sectionId);
                    container.style.display = 'none';
                    document.getElementById('searchInput').value = '';
                    history.pushState(null, null, `#${sectionId}`);
                });
            });
        }

        container.style.display = 'block';
    }

    setupScrollSpy() {
        // This would be used if we had scrollable content within sections
        // For now, we're using section switching instead
    }

    loadInitialSection() {
        const raw = window.location.hash.substring(1) || 'overview';
        const { sectionId, anchorId } = this.resolveSectionForAnchor(raw);
        this.showSection(sectionId);
        this.setActiveNavLinkById(raw);
        if (anchorId && anchorId !== sectionId) {
            const anchorEl = document.getElementById(anchorId);
            if (anchorEl) {
                anchorEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        }
    }

    showSection(sectionId) {
        const sections = document.querySelectorAll('.content-section');
        sections.forEach(section => {
            section.classList.remove('active');
        });

        const targetSection = document.getElementById(sectionId);
        if (targetSection) {
            targetSection.classList.add('active');
            // Do not force scroll here; caller decides whether to scroll to top or sub-anchor
        }
    }

    setActiveNavLink(activeLink) {
        const navLinks = document.querySelectorAll('.nav-link');
        navLinks.forEach(link => link.classList.remove('active'));
        activeLink.classList.add('active');
    }

    setActiveNavLinkById(sectionId) {
        const activeLink = document.querySelector(`a[href="#${sectionId}"]`);
        if (activeLink) {
            this.setActiveNavLink(activeLink);
        }
    }

    // Resolve which top-level content section should be shown for a given anchor id
    resolveSectionForAnchor(anchorId) {
        const el = document.getElementById(anchorId);
        if (!el) {
            // Fallback: assume anchor is a top-level section id
            return { sectionId: anchorId, anchorId };
        }
        // If the element itself is a content-section, use it directly
        if (el.classList && el.classList.contains('content-section')) {
            return { sectionId: anchorId, anchorId };
        }
        // Otherwise find the closest parent content-section
        const parentSection = el.closest('.content-section');
        if (parentSection && parentSection.id) {
            return { sectionId: parentSection.id, anchorId };
        }
        // Default fallback
        return { sectionId: anchorId, anchorId };
    }
}

// Utility functions
function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => {
        showToast('Copied to clipboard!', 'success');
    }).catch(() => {
        showToast('Failed to copy to clipboard', 'error');
    });
}

function showToast(message, type = 'info') {
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 0.75rem 1rem;
        background: ${type === 'success' ? 'var(--success-color)' : 
                    type === 'error' ? 'var(--danger-color)' : 
                    'var(--primary-color)'};
        color: white;
        border-radius: var(--border-radius);
        z-index: 10000;
        opacity: 0;
        transform: translateX(100%);
        transition: all 0.3s ease;
    `;
    toast.textContent = message;
    
    document.body.appendChild(toast);
    
    // Trigger animation
    setTimeout(() => {
        toast.style.opacity = '1';
        toast.style.transform = 'translateX(0)';
    }, 10);
    
    // Remove after 3 seconds
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(100%)';
        setTimeout(() => {
            document.body.removeChild(toast);
        }, 300);
    }, 3000);
}

// Add copy buttons to code blocks
function addCopyButtons() {
    const codeBlocks = document.querySelectorAll('.code-block');
    codeBlocks.forEach(block => {
        const copyButton = document.createElement('button');
        copyButton.innerHTML = '<i class="fas fa-copy"></i>';
        copyButton.className = 'copy-button';
        copyButton.style.cssText = `
            position: absolute;
            top: 0.5rem;
            right: 0.5rem;
            background: rgba(255,255,255,0.1);
            border: none;
            color: white;
            padding: 0.5rem;
            border-radius: 4px;
            cursor: pointer;
            opacity: 0.7;
            transition: opacity 0.2s;
        `;
        
        copyButton.addEventListener('click', () => {
            const code = block.querySelector('code').textContent;
            copyToClipboard(code);
        });
        
        copyButton.addEventListener('mouseenter', () => {
            copyButton.style.opacity = '1';
        });
        
        copyButton.addEventListener('mouseleave', () => {
            copyButton.style.opacity = '0.7';
        });
        
        block.style.position = 'relative';
        block.appendChild(copyButton);
    });
}

// Initialize the app when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new DocsApp();
    addCopyButtons();
    
    // Add smooth scrolling for any anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });
});

// Export for potential external use
window.DocsApp = DocsApp;
