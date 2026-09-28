"use client";

import { useParams } from "next/navigation";
import { ReportDetail } from "@/components/reports-panel";

export default function ReportDetailPage() {
  const params = useParams<{ clientId: string; reportId: string }>();
  return <ReportDetail clientId={params.clientId} reportId={params.reportId} />;
}
