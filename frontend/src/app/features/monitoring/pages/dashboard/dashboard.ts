import { Component, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SidebarService } from '../../../../shared/services/sidebar.service';
import { RouterModule} from '@angular/router';

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    RouterModule,
   // DashboardNavbarComponent,
    // DashboardNavbarComponent,
    // AppHeaderComponent,
    // AppSidebarComponent,
],
  templateUrl: './dashboard.html',
  //styleUrl: './dashboard.scss',
})
export class Dashboard {
  readonly isMobileOpen$;
  readonly isHovered$;
  readonly isExpanded$;
  
  // @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;

  constructor(public sidebarService: SidebarService) {
    this.isMobileOpen$ = this.sidebarService.isMobileOpen$;
    this.isHovered$ = this.sidebarService.isHovered$;
    this.isExpanded$ = this.sidebarService.isExpanded$;
  }
  get containerClasses() {
    return [
      'flex-1',
      'transition-all',
      'duration-300',
      'ease-in-out',
      (this.isExpanded$ || this.isHovered$) ? 'xl:ml-[290px]' : 'xl:ml-[90px]',
      this.isMobileOpen$ ? 'ml-0' : ''
    ];
  }

}
