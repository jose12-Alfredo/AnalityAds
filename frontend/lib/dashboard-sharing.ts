import { apiBlobRequest, apiRequest, publicApiBlobRequest, publicApiRequest } from "@/lib/api";
import type { DashboardDefinition } from "@/lib/dashboards";
export interface DashboardShare { id:string;status:string;expiresAtUtc:string|null;recipientEmail:string|null;requiresPassword:boolean;allowFilters:boolean;allowExport:boolean;allowEmbed:boolean;accessCount:number;lastAccessedAtUtc:string|null }
export interface CreatedDashboardShare { shareLink:DashboardShare;accessToken:string;relativeUrl:string }
export interface SharedDashboard { title:string;description:string;publicationNumber:number;definition:DashboardDefinition;publishedAtUtc:string;allowFilters:boolean;allowExport:boolean;allowEmbed:boolean;branding:{logoUrl:string|null;primaryColor:string;secondaryColor:string;backgroundColor:string;fontFamily:string} }
export interface DashboardExport { id:string;dashboardId:string;publicationNumber:number;format:string;status:string;fileName:string|null;errorCode:string|null;createdAtUtc:string;completedAtUtc:string|null }
export interface DeliverySchedule { id:string;dashboardId:string;recipients:string[];format:string;frequency:string;hourUtc:number;dayOfWeek:number|null;dayOfMonth:number|null;isEnabled:boolean;nextRunAtUtc:string;lastSucceededAtUtc:string|null;lastErrorCode:string|null;version:string }
export const sharingApi={
 list:(id:string,token:string)=>apiRequest<DashboardShare[]>(`/api/v1/dashboards/${id}/share-links`,token),
 create:(id:string,token:string,body:object)=>apiRequest<CreatedDashboardShare>(`/api/v1/dashboards/${id}/share-links`,token,{method:"POST",body:JSON.stringify(body)}),
 revoke:(dashboardId:string,id:string,token:string)=>apiRequest<void>(`/api/v1/dashboards/${dashboardId}/share-links/${id}`,token,{method:"DELETE"}),
 access:(accessToken:string,password:string,email:string,embed:boolean)=>publicApiRequest<SharedDashboard>("/api/v1/shared-dashboards/access",{method:"POST",body:JSON.stringify({accessToken,password:password||null,recipientEmail:email||null,embed})}),
 query:(body:object)=>publicApiRequest<{provider:string;currency:string|null;timeZone:string;rows:Array<{key:string;label:string;metrics:Record<string,{value:number|null;availability:string;unit:string}>}>}>("/api/v1/shared-dashboards/query",{method:"POST",body:JSON.stringify(body)}),
 requestExport:(id:string,token:string,format:string)=>apiRequest<DashboardExport>(`/api/v1/dashboards/${id}/exports`,token,{method:"POST",body:JSON.stringify({format})}),
 exports:(id:string,token:string)=>apiRequest<DashboardExport[]>(`/api/v1/dashboards/${id}/exports`,token),
 downloadExport:(dashboardId:string,exportId:string,token:string)=>apiBlobRequest(`/api/v1/dashboards/${dashboardId}/exports/${exportId}/file`,token),
 schedules:(id:string,token:string)=>apiRequest<DeliverySchedule[]>(`/api/v1/dashboards/${id}/delivery-schedules`,token),
 createSchedule:(id:string,token:string,body:object)=>apiRequest<DeliverySchedule>(`/api/v1/dashboards/${id}/delivery-schedules`,token,{method:"POST",body:JSON.stringify(body)}),
 deleteSchedule:(dashboardId:string,id:string,token:string)=>apiRequest<void>(`/api/v1/dashboards/${dashboardId}/delivery-schedules/${id}`,token,{method:"DELETE"}),
 publicExport:(body:object)=>publicApiRequest<DashboardExport>("/api/v1/shared-dashboards/exports",{method:"POST",body:JSON.stringify(body)}),
 publicExportStatus:(id:string,accessToken:string)=>publicApiRequest<DashboardExport>(`/api/v1/shared-dashboards/exports/${id}/status`,{method:"POST",body:JSON.stringify({accessToken})}),
 publicExportFile:(id:string,accessToken:string)=>publicApiBlobRequest(`/api/v1/shared-dashboards/exports/${id}/file`,{method:"POST",body:JSON.stringify({accessToken})}),
};
